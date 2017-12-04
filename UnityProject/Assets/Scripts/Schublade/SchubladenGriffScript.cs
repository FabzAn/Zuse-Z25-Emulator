using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SchubladenGriffScript : MonoBehaviour {

	//Der Messung nach wird die Schublade 45.3 ausgezogen
	public float ausfahrDistanz;
	public float ausfahrTempo;

	public GameObject schublade;

	float startX;

	bool inBewegung = false;
	bool ausfahren = false;
	bool ausgefahren = false;



	void Start ()
	{
		startX = schublade.transform.position.x;
	}


	void OnMouseDown ()
	{
		if(!EventSystem.current.IsPointerOverGameObject())
		{
			if (!inBewegung)
			{
				inBewegung = true;
				ausfahren = ausgefahren ? false : true;

			}
		}
	}


	void Update ()
	{
		if (inBewegung)
		{
			Vector3 neuePosition = schublade.transform.position;

			//Ausfahren bis zum gewuenschten Punkt
			if (ausfahren)
			{
				//Ich habe unkluger Weise die X-Achse nach innen zeigend gelegt, deshalb wird hier subtrahiert
				neuePosition.x -= ausfahrTempo * ausfahrDistanz * Time.deltaTime;

				if (neuePosition.x <= startX - ausfahrDistanz)
				{
					ausgefahren = true;
					inBewegung = false;
					//Nur um sicher zu gehen, dass die Schublade in keine komische Position kommt
					neuePosition.x = startX - ausfahrDistanz;
				}
			}

			//Einfahren bis zur Ausgangsposition
			else
			{
				neuePosition.x += ausfahrTempo * ausfahrDistanz * Time.deltaTime;

				if (neuePosition.x >= startX)
				{
					ausgefahren = false;
					inBewegung = false;
					//Nur um sicher zu gehen, dass die Schublade in keine komische Position kommt
					neuePosition.x = startX;
				}
			}

			schublade.transform.position = neuePosition;
		}
	}
}
