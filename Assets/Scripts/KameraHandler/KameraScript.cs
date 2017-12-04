using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KameraScript : MonoBehaviour {

	public float cameraSpeed;

	public Collider[] coll;

	Vector3 aktuelleZielPosition, vorigePosition, differenz;
	Quaternion aktuellerZielQuat;

	//Geplante ausgangs Koordinaten: -140 120 67
	Vector3 ausgangsPosition;
	//Geplante ausgangs Rotation: 155 -90 180
	Quaternion ausgangsQuat;

	bool inBewegung;
	float hoechsteRotation;


	void Start ()
	{
		if (coll == null)
			Debug.Log("coll in KameraScript nicht zugewiesen");
		else
		{
			foreach (Collider c in coll)
			{
				if (c == null)
					Debug.Log("coll in KameraScript nicht zugewiesen");
			}
		}

		ausgangsPosition = transform.position;
		ausgangsQuat = transform.rotation;
	}


	void Update ()
	{
		if (inBewegung)
		{
			kameraFahrt ();
		}
		else
		{
			//In neutraler Position kann mit gedrueckter rechter Maustaste die Kamera gedreht werden.
			if (Input.GetMouseButton(1))
			{
				float speed = 200*cameraSpeed * Time.deltaTime;
				transform.Rotate(new Vector3(-Input.GetAxis("Mouse Y"), 0, 0) * speed);
				transform.Rotate(new Vector3(0, Input.GetAxis("Mouse X"), 0) * speed, Space.World);
			}
		}
	}


	public void kameraZurueck ()
	{
		neuesZiel (ausgangsPosition, ausgangsQuat);
	}


	//Bewegt und dreht die Kamera richtung aktuelleZielPosition, aktuellerZielQuat
	void kameraFahrt ()
	{
		float step = cameraSpeed * Time.deltaTime;

		transform.position = transform.position + differenz * step;
		//Der letzte Parameter gibt an wie viel Winkel bewegt werden darf. hoechsteRotation wird benutzt um sicherzustellen,
		//dass die Rotation nicht zu schnell oder zu langsam ist
		transform.rotation = Quaternion.RotateTowards(transform.rotation, aktuellerZielQuat, hoechsteRotation * step);

		if (gleichOderDarueberhinaus())
		{
			//Korrektur der Postion, falls zu weit bewegt wurde.
			transform.position = aktuelleZielPosition;
			if (aktuellerZielQuat == transform.rotation)
				inBewegung = false;
		}
	}


	public bool neuesZiel (Vector3 neuePosition, Quaternion neuerQuaternion)
	{
		if (!inBewegung)
		{
			alleColliderEnabled();

			vorigePosition = transform.position;
			aktuelleZielPosition = neuePosition;
			differenz = neuePosition - vorigePosition;

			aktuellerZielQuat = neuerQuaternion;

			inBewegung = true;

			Vector3 winkel = neuerQuaternion.eulerAngles - transform.eulerAngles;
			winkel.x = Mathf.Abs(winkel.x);
			winkel.y = Mathf.Abs(winkel.y);
			winkel.z = Mathf.Abs(winkel.z);

			hoechsteRotation = Mathf.Max(winkel.x, winkel.y, winkel.z);

			return true;
		}
		return false;
	}


	void alleColliderEnabled ()
	{
		foreach (Collider c in coll)
		{
			c.enabled = true;
		}
	}


	//Diese Funktione ist noetig, da Transform.Translate ueber das Ziel hinausschiessen kann. Quaternion.RotateTowards kann nicht zu weit rotieren.
	bool gleichOderDarueberhinaus()
	{
		bool xKorrekt, yKorrekt, zKorrekt;
		float xDiff = transform.position.x - aktuelleZielPosition.x,
				yDiff = transform.position.y - aktuelleZielPosition.y,
				zDiff = transform.position.z - aktuelleZielPosition.z;

		//Falls ueber das Ziel hinausgeschossen wurde, sind die Vorzeichen des Bewegungsvektors und der Differenz zur jetzigen Position unterschiedlich.
		//Das wird hier fuer die Koordinaten einzeln ueberprueft.
		xKorrekt = (xDiff == 0 || Mathf.Sign(xDiff) == Mathf.Sign(differenz.x)) ? true : false;
		yKorrekt = (yDiff == 0 || Mathf.Sign(yDiff) == Mathf.Sign(differenz.y)) ? true : false;
		zKorrekt = (zDiff == 0 || Mathf.Sign(zDiff) == Mathf.Sign(differenz.z)) ? true : false;

		return xKorrekt && yKorrekt && zKorrekt;
	}
}
