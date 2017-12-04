using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class StanzerKnopfScript : MonoBehaviour {

	public FernschreiberScript fernschreiber;
	public StanzerKnopfScript andererKnopf;

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
		if (runter && transform.position.x < start.x + 1)
			transform.position = transform.position + Vector3.right * Time.deltaTime*2;
		else if (!runter && transform.position.x > start.x)
			transform.position = transform.position + Vector3.left * Time.deltaTime*2;
	}


	void OnMouseDown ()
	{
		if(!EventSystem.current.IsPointerOverGameObject())
		{
			fernschreiber.lochstreifenDrucken = wert;
			runter = true;
			andererKnopf.runter = false;
		}
	}
}
