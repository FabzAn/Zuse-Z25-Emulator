using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LochstreifenUIScript : MonoBehaviour {

	public Lochstreifen ls;

	public int position;
	public RawImage[] bausteine;
	public Texture[] texturen;

	UIHandlingScript ui;


	public void setInhalt (Lochstreifen eingabe, int neuePosition, UIHandlingScript neuUI)
	{
		ui = neuUI;

		ls = eingabe;
		position = neuePosition;
		texturenAktualisieren();
	}


	public void click ()
	{
		ui.lochstreifenClick(position, this);
	}


	public void onPointerEnter ()
	{
		ui.tooltipEin(ls.lochstreifenName);
	}


	public void onPointerExit ()
	{
		ui.tooltipAus();
	}


	void texturenAktualisieren ()
	{
		//Die korrekten Grafiken werden zugewiesen
		for (int i = 0; i < bausteine.Length; i++)
		{
			if (i < ls.inhalt.Length)
				bausteine[i].texture = texturen[ls.inhalt[i]];
			//Falls der Lochstreifen zu kurz ist
			else
				bausteine[i].gameObject.SetActive(false);
		}
	}
}
