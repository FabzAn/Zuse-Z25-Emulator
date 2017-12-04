using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LochstreifenstanzerScript : MonoBehaviour {

	public GameObject[] prefabs;

	public UIHandlingScript ui;

	List<int> lochstreifen = new List<int>();
	List<LochstreifenScript> bausteine = new List<LochstreifenScript>();

	Vector3 start = new Vector3 (-16.8f, 63.365f, 78.3f);
	Quaternion rotation = Quaternion.Euler(-90, 180, -90);

	float zeitSeitClick = -1f;


	public void stanzen (int bausteinNr)
	{
		bausteine.Add(Instantiate(prefabs[bausteinNr], start, rotation).GetComponent<LochstreifenScript>());
		foreach(LochstreifenScript l in bausteine)
			l.vorwaerts();
		lochstreifen.Add(bausteinNr);
	}


	void Update ()
	{
		if (zeitSeitClick != -1)
			zeitSeitClick += Time.deltaTime;
		if (zeitSeitClick > 3)
		{
			ui.tooltipAus();
			zeitSeitClick = -1;
		}
	}


	void OnMouseDown ()
	{
		if (zeitSeitClick == -1 || zeitSeitClick > 0.2f)
		{
			ui.tooltipEin("Doppelklick um Lochstreifen abzureißen");
			zeitSeitClick = 0;
		}
		else if (zeitSeitClick <= 0.2f)
		{
			zeitSeitClick = -1;
			if (lochstreifen.Count != 0)
				ui.lochstreifenHinzufuegen(lochstreifen.ToArray());
			ui.tooltipAus();

			foreach (LochstreifenScript l in bausteine)
				l.deletos();

			bausteine.Clear();
			lochstreifen.Clear();
		}
	}
}
