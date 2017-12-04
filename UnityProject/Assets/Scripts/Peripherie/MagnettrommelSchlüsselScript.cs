using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MagnettrommelSchlüsselScript : MonoBehaviour {

	public MagnettrommelScript trommel;

	bool ausgangsposition = true;
	int rotationsSchritte = 0;


	void OnMouseDown ()
	{
		if(!EventSystem.current.IsPointerOverGameObject())
		{
			trommel.anAus(ausgangsposition);
			ausgangsposition = !ausgangsposition;
			//Es werden 18 Schritte á 5° ausgefuehrt
			rotationsSchritte = 18;
		}
	}


	void Update ()
	{
		if (rotationsSchritte > 0)
		{
			transform.RotateAround(transform.position, transform.right, (ausgangsposition ? 5 : -5));
			rotationsSchritte--;
		}
	}
}
