using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HandgunController : MonoBehaviour
{

	Animator anim;
	[SerializeField] private SharedInt _AmmoMag    = null;
	[SerializeField] private SharedInt _AmmoTotal    = null;
	[SerializeField] private SharedFloat _stamina = null;
	[SerializeField] private float FireRate = 1.0f;
	[SerializeField] private GameObject Crosshair = null;
	[SerializeField] private Text Inv_Mag = null;
	[SerializeField] private Text Inv_Ammo = null;
	private float NextTimeToFire = 0.0f;
	
	void Start()
	{
		anim = gameObject.GetComponent<Animator>();
	}

    // Update is called once per frame
    void Update()
    {

		while(Input.GetButton("Horizontal") || Input.GetButton("Vertical"))
		{
			anim.SetBool("isWalking", true);
			break;
		}

		while(!Input.GetButton("Horizontal") && !Input.GetButton("Vertical"))
		{
			anim.SetBool("isWalking", false);
			break;
		}

		if(Input.GetButtonDown("Sprint") && _stamina.value>30.0f)
		{
			anim.SetBool("isRunning", true);
			Crosshair.SetActive(false);
		}
		else if(Input.GetButtonUp("Sprint"))
		{
			anim.SetBool("isRunning", false);
			Crosshair.SetActive(true);
		}

		if(Input.GetButtonDown("Aim"))
		{
			anim.SetBool("isAiming", true);
			Crosshair.SetActive(false);
		}
		else if(Input.GetButtonUp("Aim"))
		{
			anim.SetBool("isAiming", false);
			Crosshair.SetActive(true);
		}

		if(Input.GetButtonDown("Fire1") && Time.time >=NextTimeToFire && _AmmoMag.value>0 && !anim.GetCurrentAnimatorStateInfo(0).IsName("Reload"))
		{
			anim.SetTrigger("Fire");
			NextTimeToFire = Time.time + 1.0f/FireRate;
			_AmmoMag.value = _AmmoMag.value-1;
			Inv_Mag.text = _AmmoMag.value.ToString();
		}

		if(Input.GetButtonDown("Reload") && _AmmoMag.value<9 && _AmmoTotal.value>0)
		{
			anim.SetTrigger("isReloading");
			if (_AmmoTotal.value > 9)
			{
				_AmmoTotal.value = _AmmoTotal.value - (9 - _AmmoMag.value);
				_AmmoMag.value = 9;
			}

			else
			{	
				if ((_AmmoTotal.value + _AmmoMag.value)<=9)
				{
					_AmmoMag.value = _AmmoMag.value + _AmmoTotal.value;
					_AmmoTotal.value = 0;
				}
				else
				{
					_AmmoTotal.value = _AmmoTotal.value - (9 - _AmmoMag.value);
					_AmmoMag.value = 9;
				}
			}

			Inv_Mag.text = _AmmoMag.value.ToString();
			Inv_Ammo.text = _AmmoTotal.value.ToString();
		}



			
		/*
		if(Input.GetButtonDown("Hide") && anim.GetBool("isHidden"))
		{
			anim.SetBool("isHidden", false);
		}

		else if(Input.GetButtonDown("Hide") && !anim.GetBool("isHidden"))
		{
			anim.SetBool("isHidden", true);
		}
		*/
    }
}
