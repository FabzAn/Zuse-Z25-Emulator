using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class LeserKnopfScript : MonoBehaviour {

	public LochstreifenleserScript leser;
	public LeserKnopfScript andererKnopf;

	//Gibt an, ob die Taste das Stanzen ein- oder ausschaltet
	public bool wert;

	Vector3 start;

	//true wenn dieser Knopf gedrueckt ist, sonst false
	public bool runter = false;


	void Start ()
	{
		start = transform.position;
	}


	void Update ()
	{
		if (runter && transform.position.y > start.y - 0.5f)
			transform.position = transform.position + Vector3.down * Time.deltaTime*2;
		else if (!runter && transform.position.y < start.y)
			transform.position = transform.position + Vector3.up * Time.deltaTime*2;
	}


	void OnMouseDown ()
	{
		if(!EventSystem.current.IsPointerOverGameObject())
		{
			leser.knopfGedrueckt = wert;
			runter = true;
			andererKnopf.runter = false;
		}
	}
}
