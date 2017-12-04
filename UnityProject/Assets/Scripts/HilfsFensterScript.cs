using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HilfsFensterScript : MonoBehaviour {


	public GameObject hilfeFenster;
	public GameObject[] optionen;


	public void dropDownHandling (int input)
	{
		foreach (GameObject g in optionen)
			g.SetActive(false);

		optionen[input].SetActive(true);
	}


	public void knopfHandling ()
	{
		hilfeFenster.SetActive(!hilfeFenster.activeSelf);
	}
}
