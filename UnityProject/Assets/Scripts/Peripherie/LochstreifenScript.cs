using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LochstreifenScript : MonoBehaviour {

	public int phase = 0;
	public int wert;

	bool loeschen = false;



	//Objekt wird im LateUpdate zerstoert, damit im Update des Lochstreifenlesers kein Error geworfen wird
	void LateUpdate ()
	{
		if (loeschen)
			Destroy(gameObject);
	}


	//Startkoordinaten (-22.41, 63.6, 25.388)
	//Lochstreifen Zeile wird gelesen an Koordinate (-22.41, 63.6, 31.23) also nach 23 Einheiten
	//letzte gerade Stelle (-22.41, 63.6, 32.246) nach 27 Einheiten
	//Danach (-23.487, 65.268, 33.326) mit Rotation (90, 270, 0)
	//(-23.735, 65.246, 33.326) mit Rotation (100, 270, 0)
	//(-23.979, 65.18, 33.326) mit Rotation (110, 270, 0)
	//Dann wieder vorwaerts um 0.254


	//schritt wird vom Lochstreifenleser gecallt. Der Rueckgabewert ist -1, ausser der Lochstreifen soll gerade gelesen werden
	public int schritt ()
	{
		phase++;
		if (phase <= 27)
		{
			transform.position = transform.position + transform.up.normalized * 0.254f;

			if (phase == 23)
				return wert;
		}
		else if (phase == 28)
		{
			transform.position = new Vector3 (-23.487f, 65.268f, 33.326f);
			transform.rotation = Quaternion.Euler(90, 270, 0);
		}
		else if (phase == 29)
		{
			transform.position = new Vector3 (-23.735f, 65.246f, 33.326f);
			transform.rotation = Quaternion.Euler(100, 270, 0);
		}
		else if (phase == 30)
		{
			transform.position = new Vector3 (-23.979f, 65.18f, 33.326f);
			transform.rotation = Quaternion.Euler(110, 270, 0);
		}
		else
		{
			transform.position = transform.position + transform.up.normalized * 0.254f;
		}

		return -1;
	}


	public void vorwaerts ()
	{
		transform.position = transform.position + transform.up.normalized * 0.254f;
	}


	public void deletos ()
	{
		loeschen = true;
	}
}
