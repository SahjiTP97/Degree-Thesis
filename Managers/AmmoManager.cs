using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AmmoManager : MonoBehaviour
{

	[SerializeField] private Text		_MagAmmoText   =	null;
	[SerializeField] private Text		_TotalAmmoText		=	null;
	[SerializeField] private SharedInt _AmmoMag    = null;
	[SerializeField] private SharedInt _AmmoTotal    = null;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
		_MagAmmoText.text = _AmmoMag.value.ToString();
		_TotalAmmoText.text = _AmmoTotal.value.ToString();
    }
}
