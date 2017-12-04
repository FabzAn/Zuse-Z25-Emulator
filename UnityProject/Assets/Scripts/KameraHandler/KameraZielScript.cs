using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class KameraZielScript : MonoBehaviour {

	public Vector3 position, rotation;
	Quaternion rotationQuat;

	public KameraScript cam;
	Collider coll;


	void Start ()
	{
		rotationQuat = Quaternion.Euler(rotation);

		coll = GetComponent<Collider>();

		if (cam == null)
			cam = GameObject.Find("Main Camera").GetComponent<KameraScript>();
	}


	void OnMouseDown ()
	{
		if(!EventSystem.current.IsPointerOverGameObject())
		{
			kameraHierher();
		}
	}


	public void kameraHierher ()
	{
		if (cam.neuesZiel(position, rotationQuat))
			coll.enabled = false;
	}
}
