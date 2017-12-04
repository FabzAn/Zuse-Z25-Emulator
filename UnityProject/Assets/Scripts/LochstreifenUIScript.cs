using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LochstreifenUIScript : MonoBehaviour {

	public int[] inhalt;
	public int position;
	public RawImage[] bausteine;
	public Texture[] texturen;

	UIHandlingScript ui;


	public void setInhalt (int[] eingabe, int neuePosition, UIHandlingScript neuUI)
	{
		ui = neuUI;

		inhalt = eingabe;
		position = neuePosition;
		texturenAktualisieren();
	}


	public void click ()
	{
		ui.lochstreifenClick(position, inhalt);
	}


	void texturenAktualisieren ()
	{
		//Die korrekten Grafiken werden zugewiesen
		for (int i = 0; i < bausteine.Length; i++)
		{
			if (i < inhalt.Length)
				bausteine[i].texture = texturen[inhalt[i]];
			//Falls der Lochstreifen zu kurz ist
			else
				bausteine[i].gameObject.SetActive(false);
		}
	}
}
