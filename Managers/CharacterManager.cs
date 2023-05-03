using UnityEngine;
using System.Collections;

public class CharacterManager : MonoBehaviour 
{
    // Inspector Assigned
    [SerializeField] private CapsuleCollider 	_meleeTrigger 		= null;
	[SerializeField] private CameraBloodEffect	_cameraBloodEffect 	= null;
	[SerializeField] private Camera				_camera				= null;
	[SerializeField] private AISoundEmitter		_soundEmitter		= null;
	[SerializeField] private float				_walkRadius			= 0.0f;
	[SerializeField] private float				_runRadius			= 7.0f;
	[SerializeField] private float				_landingRadius		= 12.0f;
	[SerializeField] private float				_bloodRadiusScale	= 6.0f;
	[SerializeField] private PlayerHUD			_playerHUD			= null;

	// Pain Damage Audio
	[SerializeField] private AudioCollection	_damageSounds		= null;
	[SerializeField] private AudioCollection	_painSounds			= null;
	[SerializeField] private AudioCollection	_tauntSounds		= null;

	[SerializeField] private float				_nextPainSoundTime	=	0.0f;
	[SerializeField] private float				_painSoundOffset	=	0.35f;
	[SerializeField] private float				_tauntRadius		= 	10.0f;

    [Header("Inventory")]
    [SerializeField] private GameObject _inventoryUI = null;
    [SerializeField] private Inventory _inventory = null;

    [Header("Shared Variables")]
    [SerializeField] private SharedFloat _health    = null;
    [SerializeField] private SharedFloat _infection = null;
    [SerializeField] private SharedString _interactionText = null;
	[SerializeField] private SharedInt _AmmoMag = null;

    // Private
    private Collider 			_collider 			 = null;
	private FPSController		_fpsController 		 = null;
	private CharacterController _characterController = null;
	private GameSceneManager	_gameSceneManager	 = null;
	private int					_aiBodyPartLayer     = -1;
	private int 				_interactiveMask	 = 0;
	private float				_nextAttackTime		 = 0;
	private float				_nextTauntTime		 = 0;

	public FPSController	fpsController	{ get{ return _fpsController;}}

	// Use this for initialization
	void Start () 
	{
		_collider 			= GetComponent<Collider>();
		_fpsController 		= GetComponent<FPSController>();
		_characterController= GetComponent<CharacterController>();
		_gameSceneManager 	= GameSceneManager.instance;
		_aiBodyPartLayer 	= LayerMask.NameToLayer("AI Body Part");
		_interactiveMask	= 1 << LayerMask.NameToLayer("Interactive");

		if (_gameSceneManager!=null)
		{
			PlayerInfo info 		= new PlayerInfo();
			info.camera 			= _camera;
			info.characterManager 	= this;
			info.collider			= _collider;
			info.meleeTrigger		= _meleeTrigger;

			_gameSceneManager.RegisterPlayerInfo( _collider.GetInstanceID(), info );
		}

		// Get rid of really annoying mouse cursor
		Cursor.visible = false;
		Cursor.lockState = CursorLockMode.Locked;

		// Start fading in
		if (_playerHUD) _playerHUD.Fade( 2.0f, ScreenFadeType.FadeIn );
	}
    private void OnEnable()
    {
        // Register Inventory Listeners
        if (_inventory)
        {
            _inventory.OnWeaponChange.AddListener(OnSwitchWeapon);
            _inventory.OnWeaponDropped.AddListener(OnDropWeapon);
        }
    }

    private void OnDisable()
    {
        // Unregister Inventory Listeners
        if (_inventory)
        {
            _inventory.OnWeaponChange.RemoveListener(OnSwitchWeapon);
            _inventory.OnWeaponDropped.RemoveListener(OnDropWeapon);
        }
    }

    private void OnDropWeapon(InventoryItemWeapon weapon)
    {
        Debug.Log("Dropping Weapon");
    }

    private void OnSwitchWeapon(InventoryWeaponMountInfo wmi)
    {
        if (_inventory)
        {
            _inventory.DropWeaponItem(wmi.Weapon.weaponType == InventoryWeaponType.SingleHanded ? 0 : 1);

            int mountIndex = wmi.Weapon.weaponType == InventoryWeaponType.SingleHanded ? 0 : 1;
            _inventory.AssignWeapon(mountIndex, wmi);
        }
    }
    public void TakeDamage ( float amount, bool doDamage, bool doPain )
	{
		_health.value = Mathf.Max ( _health.value - (amount *Time.deltaTime)  , 0.0f);

		if (_fpsController)
		{
			_fpsController.dragMultiplier = 0.0f; 

		}
		if (_cameraBloodEffect!=null)
		{
			_cameraBloodEffect.minBloodAmount = (1.0f - _health.value/100.0f) * 0.5f;
			_cameraBloodEffect.bloodAmount = Mathf.Min(_cameraBloodEffect.minBloodAmount + 0.3f, 1.0f);	
		}

		// Do Pain / Damage Sounds
		if (AudioManager.instance)
		{
			if (doDamage && _damageSounds!=null)
				AudioManager.instance.PlayOneShotSound( _damageSounds.audioGroup,
														_damageSounds.audioClip, transform.position,
														_damageSounds.volume,
														_damageSounds.spatialBlend,
														_damageSounds.priority );

			if (doPain && _painSounds!=null && _nextPainSoundTime<Time.time)
			{
				AudioClip painClip = _painSounds.audioClip;
				if (painClip)
				{
					_nextPainSoundTime = Time.time + painClip.length;
					StartCoroutine(AudioManager.instance.PlayOneShotSoundDelayed(	_painSounds.audioGroup,
																			 	 	painClip,
																			  		transform.position,
																			  		_painSounds.volume,
																			  		_painSounds.spatialBlend,
																			  		_painSoundOffset,
																			  		_painSounds.priority ));
				}
			}
		}

		if (_health.value<=0.0f) 
		{
			DoDeath();
		}
	}

	public void DoDamage( int hitDirection = 0 )
	{
		if (_camera==null) return;
		if (_gameSceneManager==null) return;

		// Local Variables
		Ray ray;
		RaycastHit hit;
		bool isSomethingHit	=	false;

		ray = _camera.ScreenPointToRay( new Vector3( Screen.width/2, Screen.height/2, 0 ));

		isSomethingHit = Physics.Raycast( ray, out hit, 100.0f, 1<<_aiBodyPartLayer );

		if (isSomethingHit)
		{
			AIStateMachine stateMachine = _gameSceneManager.GetAIStateMachine( hit.rigidbody.GetInstanceID());
			if (stateMachine)
			{
				stateMachine.TakeDamage( hit.point, ray.direction * 1.0f, 50, hit.rigidbody, this, 0 );
				_nextAttackTime = Time.time+0.8f;
			}
		}

	}

	void Update()
	{
        // Process Inventory Key Toggle
        if (Input.GetButtonDown("Inventory") && _inventoryUI)
        {
            // If its not visible...make it visible
            if (!_inventoryUI.activeSelf)
            {
                _inventoryUI.SetActive(true);
                if (_playerHUD) _playerHUD.gameObject.SetActive(false);
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                return;
            }
            else
            {
                _inventoryUI.SetActive(false);
                if (_playerHUD) _playerHUD.gameObject.SetActive(true);
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }


        Ray ray;
		RaycastHit hit;
		RaycastHit [] hits;
		
		// PROCESS INTERACTIVE OBJECTS
		// Is the crosshair over a usuable item or descriptive item...first get ray from centre of screen
		ray = _camera.ScreenPointToRay( new Vector3(Screen.width/2, Screen.height/2, 0));

		// Calculate Ray Length
		float rayLength =  Mathf.Lerp( 1.0f, 1.8f, Mathf.Abs(Vector3.Dot( _camera.transform.forward, Vector3.up )));

		// Cast Ray and collect ALL hits
		hits = Physics.RaycastAll (ray, rayLength, _interactiveMask );

		// Process the hits for the one with the highest priorty
		if (hits.Length>0)
		{
			// Used to record the index of the highest priorty
			int 				highestPriority = int.MinValue;
			InteractiveItem		priorityObject	= null;	

			// Iterate through each hit
			for (int i=0; i<hits.Length; i++)
			{
				// Process next hit
				hit = hits[i];
               
				// Fetch its InteractiveItem script from the database
				InteractiveItem interactiveObject = _gameSceneManager.GetInteractiveItem( hit.collider.GetInstanceID());

				// If this is the highest priority object so far then remember it
				if (interactiveObject!=null && interactiveObject.priority>highestPriority)
				{
					priorityObject = interactiveObject;
					highestPriority= priorityObject.priority;
				}
			}

			// If we found an object then display its text and process any possible activation
			if (priorityObject!=null)
			{
                if (_interactionText)
                    _interactionText.value = priorityObject.GetText();
                
				if (Input.GetButtonDown ( "Use" ))
				{
					priorityObject.Activate( this );
				}
			}
		}
		else
		{
            if (_interactionText)
                _interactionText.value = null;
        }

		// Are we attacking?
		if (Input.GetMouseButtonDown(0) && Time.time>_nextAttackTime && _AmmoMag.value>0)
		{
			DoDamage();
		}



		// Calculate the SoundEmitter radius and the Drag Multiplier Limit
		if (_fpsController && _soundEmitter!=null)
		{
			float newRadius = Mathf.Max( _walkRadius, (100.0f-_health.value)/_bloodRadiusScale);
			switch (_fpsController.movementStatus)
			{
				case PlayerMoveStatus.Landing: newRadius = Mathf.Max( newRadius, _landingRadius ); break;
				case PlayerMoveStatus.Running: newRadius = Mathf.Max( newRadius, _runRadius ); break;
			}

			_soundEmitter.SetRadius( newRadius );

			_fpsController.dragMultiplierLimit = Mathf.Max(_health.value/100.0f, 0.25f);
		}


		// Do Insult
		if (Input.GetButtonDown("Taunt"))
		{
			DoTaunt();
		}

	}


	void DoTaunt()
	{
		if (_tauntSounds==null || Time.time<_nextTauntTime || !AudioManager.instance) return;
		AudioClip taunt = _tauntSounds[0];
		AudioManager.instance.PlayOneShotSound( _tauntSounds.audioGroup, 
												taunt, 
												transform.position, 
												_tauntSounds.volume, 
												_tauntSounds.spatialBlend,
												_tauntSounds.priority
												 );
		if (_soundEmitter!=null)
			_soundEmitter.SetRadius(_tauntRadius); 
		_nextTauntTime = Time.time+taunt.length;
	}


	public void DoLevelComplete()
	{
		if (_fpsController) 
			_fpsController.freezeMovement = true;

	/*	if (_playerHUD)
		{
			_playerHUD.Fade( 4.0f, ScreenFadeType.FadeOut );
			_playerHUD.ShowMissionText( "Mission Completed");
			_playerHUD.Invalidate(this);
		}*/

		Invoke( "GameOver", 4.0f);
	}


	public void DoDeath()
	{
		if (_fpsController) 
			_fpsController.freezeMovement = true;

		/*if (_playerHUD)
		{
			_playerHUD.Fade( 3.0f, ScreenFadeType.FadeOut );
			_playerHUD.ShowMissionText( "Mission Failed");
			_playerHUD.Invalidate(this);
		}*/

		Invoke( "GameOver", 3.0f);
	}

	void GameOver()
	{
		// Show the cursor again
		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.None;

		if (ApplicationManager.instance)
			ApplicationManager.instance.LoadMainMenu();
	}
}
