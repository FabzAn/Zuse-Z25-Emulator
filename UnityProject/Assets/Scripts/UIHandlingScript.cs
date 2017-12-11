using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class UIHandlingScript : MonoBehaviour {

	public RechenwerkScript rechenWerk;
	public MagnettrommelScript magnetTrommel;

	public Text buttonAufschrift;
	public RectTransform uIWrapperTransform;

	public GameObject speicherUebersicht;
	public GameObject befehlsUebersicht;
	public GameObject optionen;
	public GameObject lochstreifenMenu;

	public GameObject[] befehlsbeschreibungen;
	public GameObject befehlsKnoepfe;
	public GameObject zurueckKnopf;

	public GameObject tooltipImg;
	RectTransform tooltipImgRect;
	public Text tooltipText;

	[Tooltip("x Koordinate wenn ausgeklappt")]
	public int ausgeklapptesX;
	[Tooltip("Ausklapp Tempo in Pixel pro Sekunde")]
	public float ausklappGeschwindigkeit;

	public string dateiName {get; set;}
	public GameObject dateiNameInputField;
	public GameObject speichernPrompt;
	public GameObject speichernBestaetigen;
	public GameObject ladenPrompt;
	public GameObject ladenFehlgeschlagen;
	public GameObject keinDateiZugriff;

	//Nochmal das alles fuer das Speichern von Lochstreifen
	public string dateiNameLochstreifen {get; set;}
	public GameObject dateiNameInputFieldLochstreifen;
	public GameObject speichernPromptLochstreifen;
	public GameObject speichernBestaetigenLochstreifen;
	public GameObject ladenPromptLochstreifen;
	public GameObject ladenFehlgeschlagenLochstreifen;
	public GameObject keinDateiZugriffLochstreifen;

	public string neuerNameLochstreifen { get; set; }
	public GameObject neuerNameInputField;
	public GameObject neuerNamePrompt;

	public Transform lochstreifenPrefab;

	public Transform lochstreifenWrapper;
	public RectTransform lochstreifenWrapperRect;
	public RectTransform markerLochstreifenMenu;

	public KnopfScript[] knoepfe;


	LochstreifenUIScript gewaehlterLochstreifen;
	//-1 heisst kein Lochstreifen ist gewaehlt
	int lochstreifenPosition = -1;
	int anzahlLochstreifen = 0;


	bool ausgeklappt = false;
	CanvasScaler scaler;

	float tooltipAusschaltenIn = 0f;



	void Start ()
	{
		scaler = GameObject.Find("Canvas").GetComponent<CanvasScaler>();

		if (rechenWerk == null)
			Debug.Log("rechenWerk in UIHandlingScript nicht zugewiesen");

		if (buttonAufschrift == null)
			Debug.Log("buttonAufschrift nicht zugewiesen");

		if (uIWrapperTransform == null)
			Debug.Log("uIWrapperTransform nicht zugewiesen");

		if (befehlsbeschreibungen == null)
			Debug.Log("befehlsbeschreibungen in UI Wrapper nicht zugewiesen");

		if (zurueckKnopf == null)
			Debug.Log("zurueckKnopf in UI Wrapper nicht zugewiesen");

		if (tooltipImg == null || tooltipText == null)
			Debug.Log("tooltip in UI Wrapper nicht zugewiesen");
		else
			tooltipImgRect = tooltipImg.GetComponent<RectTransform>();
	}



	void Update ()
	{
		Vector2 position = uIWrapperTransform.anchoredPosition;

		//UI ist im Begriff aus- oder eingeklappt zu werden
		if ( (ausgeklappt && position.x < ausgeklapptesX) || (!ausgeklappt && position.x > 0) )
		{
			position.x += (ausgeklappt ? 1 : -1) * Time.deltaTime * ausklappGeschwindigkeit;

			//Damit das UI nicht zu weit ein- oder ausfaehrt
			if (position.x < 0)
				position.x = 0;
			else if (position.x > ausgeklapptesX)
				position.x = ausgeklapptesX;

			uIWrapperTransform.anchoredPosition = position;
		}

		if (tooltipAusschaltenIn != 0)
		{
			if (tooltipAusschaltenIn > 0)
				tooltipAusschaltenIn -= Time.deltaTime;
			else
			{
				tooltipAusschaltenIn = 0;
				tooltipAus();
			}
		}
	}



	public void einAusKlappen ()
	{
		ausgeklappt = !ausgeklappt;
		//Der Knopf zeigt << oder >>
		buttonAufschrift.text = (ausgeklappt ? "<<" : ">>");
	}


	public void dropDownHandling (int input)
	{
		speicherUebersicht.SetActive(false);
		befehlsUebersicht.SetActive(false);
		lochstreifenMenu.SetActive(false);
		optionen.SetActive(false);
		switch (input)
		{
			case 0: speicherUebersicht.SetActive(true); break;
			case 1: befehlsUebersicht.SetActive(true); break;
			case 2: lochstreifenMenu.SetActive(true); break;
			case 3: optionen.SetActive(true); break;
			default: Debug.Log("MenueDropdownError"); break;
		}
	}


	public void audioOnOff (bool input)
	{
		AudioListener.volume = (input ? 1 : 0);
	}


	public void knopfTexturSwitch (bool input)
	{
		foreach (KnopfScript knopf in knoepfe)
		{
			knopf.switchTexture(input);
		}

	}


	public void turnOff ()
	{
		Application.Quit();
	}


	public void beschreibungZeigen (int nr)
	{
		befehlsKnoepfe.SetActive(false);

		befehlsbeschreibungen[nr].SetActive(true);
		zurueckKnopf.SetActive(true);
	}


	public void beschreibungZurueck ()
	{
		befehlsKnoepfe.SetActive(true);

		for (int i = 0; i < befehlsbeschreibungen.Length; i++)
			befehlsbeschreibungen[i].SetActive(false);
		zurueckKnopf.SetActive(false);
	}


	public void speichern (bool ueberschreiben)
	{
		Speichereinheit einheit = new Speichereinheit(magnetTrommel.gesamtenInhaltAuslesen(), rechenWerk.speicherGet());

		BinaryFormatter bf = new BinaryFormatter();
		string pfad = Application.dataPath + "/Saves/" + dateiName + ".z25";

		Directory.CreateDirectory(Application.dataPath + "/Saves/");

		if (!ueberschreiben && File.Exists(pfad))
		{
			speichernBestaetigen.SetActive(true);
		}
		else
		{
			try
			{
				FileStream file = File.Create(pfad);
				bf.Serialize(file, einheit);
				file.Close();
			}
			catch (UnauthorizedAccessException e)
			{
				keinDateiZugriff.SetActive(true);
			}

			speichernPrompt.SetActive(false);
			speichernBestaetigen.SetActive(false);
			dateiNameInputField.SetActive(false);
		}
	}


	public void laden ()
	{
		BinaryFormatter bf = new BinaryFormatter();
		string pfad = Application.dataPath + "/Saves/" + dateiName + ".z25";

		if (File.Exists(pfad))
		{
			FileStream file = File.Open(pfad, FileMode.Open, FileAccess.Read);
			Speichereinheit einheit = (Speichereinheit) bf.Deserialize(file);


			rechenWerk.speicherSet(einheit.getSpeicher());
			magnetTrommel.gesamtenInhaltSchreiben(einheit.getTrommel());
			file.Close();

			ladenPrompt.SetActive(false);
			ladenFehlgeschlagen.SetActive(false);
			dateiNameInputField.SetActive(false);
		}
		else
		{
			ladenFehlgeschlagen.SetActive(true);
		}
	}


	public void tooltipEin (string inhalt)
	{
		tooltipImg.SetActive(true);

		//Pivot Point wird so veraendert, dass das Fenster immer auf der Seite der Maus ist, die zur Mitte des Bildschirms zeigt
		float mousePivotx = (Input.mousePosition.x > (Screen.width / 2)) ? 1f : 0f;
		float mousePivoty = (Input.mousePosition.y > (Screen.height / 2)) ? 1f : 0f;
		tooltipImgRect.pivot = new Vector2(mousePivotx, mousePivoty);

		//Die Mausposition wird in den richtigen Maßstab transformiert
		tooltipImgRect.anchoredPosition = new Vector2(Input.mousePosition.x * (scaler.referenceResolution.x / Screen.width),
															Input.mousePosition.y * (scaler.referenceResolution.y / Screen.height));
		tooltipText.text = inhalt;

		tooltipAusschaltenIn = 10f;
	}


	public void tooltipAus ()
	{
		tooltipImg.SetActive(false);
	}


	//Das Speichern und Laden von Lochstreifen funktioniert genau wie das regulaere Speichern/Laden
	public void lochstreifenSpeichern (bool ueberschreiben)
	{
		if (lochstreifenPosition == -1)
		{
			keinLochstreifen();
			return;
		}


		BinaryFormatter bf = new BinaryFormatter();
		string pfad = Application.dataPath + "/Saves/" + dateiNameLochstreifen + ".ls";

		Directory.CreateDirectory(Application.dataPath + "/Saves/");

		if (!ueberschreiben && File.Exists(pfad))
		{
			speichernBestaetigenLochstreifen.SetActive(true);
		}
		else
		{
			try
			{
				FileStream file = File.Create(pfad);
				bf.Serialize(file, gewaehlterLochstreifen.ls);
				file.Close();
			}
			catch (UnauthorizedAccessException e)
			{
				keinDateiZugriffLochstreifen.SetActive(true);
			}

			speichernPromptLochstreifen.SetActive(false);
			speichernBestaetigenLochstreifen.SetActive(false);
			dateiNameInputFieldLochstreifen.SetActive(false);
		}
	}


	public void lochstreifenLaden ()
	{
		BinaryFormatter bf = new BinaryFormatter();
		string pfad = Application.dataPath + "/Saves/" + dateiNameLochstreifen + ".ls";

		if (File.Exists(pfad))
		{
			FileStream file = File.Open(pfad, FileMode.Open, FileAccess.Read);

			Lochstreifen neu;

			try
			{
				neu = (Lochstreifen) bf.Deserialize(file);
			}
			//Exception um alte ls Dateien (noch als int[] gespeichert) lesen zu koennen
			catch (InvalidCastException e)
			{
				file.Close();
				file = File.Open(pfad, FileMode.Open, FileAccess.Read);
				int[] tmp = (int[]) bf.Deserialize(file);
				neu = new Lochstreifen(tmp);
			}

			file.Close();

			lochstreifenHinzufuegen(neu);

			ladenPromptLochstreifen.SetActive(false);
			ladenFehlgeschlagenLochstreifen.SetActive(false);
			dateiNameInputFieldLochstreifen.SetActive(false);
		}
		else
		{
			ladenFehlgeschlagenLochstreifen.SetActive(true);
		}
	}


	public void namenAendernOeffnen ()
	{
		neuerNamePrompt.SetActive(true);
		neuerNameInputField.GetComponent<InputField>().text = gewaehlterLochstreifen.ls.lochstreifenName;
	}


	public void lochsteifenNamenAendern ()
	{
		if (lochstreifenPosition == -1)
		{
			keinLochstreifen();
			return;
		}

		gewaehlterLochstreifen.ls.lochstreifenName = neuerNameLochstreifen;

		neuerNamePrompt.SetActive(false);
	}


	public int[] getAktuellenLochstreifen ()
	{
		/*Vorlauefige Test ausgabe
		return new []{31, 7, 27, 23, 22, 22, 31, 7, 4, 31, 3, 27, 23, 22, 4, 31, 5, 16, 27, 22, 4, 31, 1, 27, 23, 22, 22, 0, 1, 4,
					0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
					0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
					0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
					0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
					0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
					0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
					0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
					0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
					0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
					0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
					0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
					0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
		*/

		if (lochstreifenPosition == -1)
		{
			keinLochstreifen();
			return new []{-1};
		}
		else
			return gewaehlterLochstreifen.ls.inhalt;
	}


	public void lochstreifenHinzufuegen (Lochstreifen eingabe)
	{
		Transform neuerLochstreifen = Instantiate(lochstreifenPrefab, lochstreifenWrapper);
		neuerLochstreifen.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -100*anzahlLochstreifen);
		neuerLochstreifen.GetComponent<LochstreifenUIScript>().setInhalt(eingabe, anzahlLochstreifen, this);
		anzahlLochstreifen++;

		//Laenge der Uebersicht anpassen
		lochstreifenWrapperRect.sizeDelta = new Vector2(380, 100*anzahlLochstreifen);
	}


	public void lochstreifenHinzufuegen (int[] eingabe)
	{
		lochstreifenHinzufuegen(new Lochstreifen(eingabe, "Unbenannt"));
	}


	public void lochstreifenClick (int position, LochstreifenUIScript ausgewaehlterLochstreifen)
	{
		lochstreifenPosition = position;
		gewaehlterLochstreifen = ausgewaehlterLochstreifen;

		//Marker Position anpassen;
		Vector2 neueMarkerPosition = markerLochstreifenMenu.anchoredPosition;
		neueMarkerPosition.y = -50 - 100*position;
		markerLochstreifenMenu.anchoredPosition = neueMarkerPosition;

		//Sorgt dafuer, dass der Marker ganz oben gezeichnet wird
		markerLochstreifenMenu.transform.SetAsLastSibling();
	}


	void keinLochstreifen ()
	{
		tooltipEin("Bitte zunaechst einen Lochstreifen waehlen");
	}
}
