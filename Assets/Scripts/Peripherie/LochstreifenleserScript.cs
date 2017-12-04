using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class LochstreifenleserScript : PeripherieScript {

	public FernschreiberScript fernschreiber;
	public GameObject lochstreifenRolle, umklapp;
	//Die 32 Lochstreifen Bausteine
	public GameObject[] prefabs;
	public UIHandlingScript ui;

	public AudioSource audio;


	List<Wort> outputBuffer = new List<Wort>();
	string befehlsBuffer = "";

	bool eingabeIstText = false, einApostroph = false;
	bool buchstabenEingabe = true;
	int zeichenZaehler = 0;

	bool bandBefehlStehtNochAus = false;
	int bandBefehlZieladresse = 0;

	float zeitpunkt = 0;
	List<LochstreifenScript> lochstreifen = new List<LochstreifenScript>();

	int[] gegebenerLochstreifen;
	//-1 bedeutet, kein Lochstreifen vorhanden
	int positionImLochstreifen = -1;


	public enum Modus
	{
		Warten,
		Bringtransfer,
		Schaltimpuls
	}
	Modus _modus;
    Modus modus
    {
        get {return _modus;}
        set
        {
            _modus = value;

            if (_modus == Modus.Warten)
            {
				audio.Stop();

                outputBuffer.Clear();
				bandBefehlStehtNochAus = false;
				bandBefehlZieladresse = 0;
				befehlsBuffer = "";
				eingabeIstText = false;
				einApostroph = false;
				buchstabenEingabe = true;
				zeichenZaehler = 0;
				zeitpunkt = 0;
				peripherie.freigabe = false;

				knopfGedrueckt = false;
				knopfAn.runter = false;
				knopfAus.runter = true;
            }
        }
	}


	bool _knopfGedrueckt;
	//Ist true wenn An Knopf gedrueckt ist
	public bool knopfGedrueckt
	{
		get {return _knopfGedrueckt;}
		set
		{
			_knopfGedrueckt = value;
			audio.Stop();
		}
	}

	public LeserKnopfScript knopfAn;
	public LeserKnopfScript knopfAus;

	//Startkoordinaten (-22.41, 63.6, 25.388)
	Vector3 start = new Vector3 (-22.41f, 63.6f, 25.388f);
	Quaternion rotation = Quaternion.Euler(new Vector3(-90, 180,0));
	//Lochstreifen Zeile wird gelesen an Koordinate (-22.41, 63.6, 31.23) also nach 23 Einheiten
	//letzte gerade Stelle (-22.41, 63.6, 32.246) nach 27 Einheiten
	//Danach (-23.487, 65.268, 33.326) mit Rotation (90, 270, 0)
	//(-23.735, 65.246, 33.326) mit Rotation (100, 270, 0)
	//(-23.979, 65.18, 33.326) mit Rotation (110, 270, 0)
	//Dann wieder vorwaerts um 0.254
	//Vier ragen ueber beim Start


	void Update ()
	{
		if (modus != Modus.Warten && knopfGedrueckt)
		{
			zeitpunkt += Time.deltaTime;

			int a = -1, b = -1;

			if (zeitpunkt >= 0.1f && positionImLochstreifen != -1)
			{
				if (!audio.isPlaying)
					audio.Play();


				foreach (LochstreifenScript l in lochstreifen)
				{
					//Der hoechste Wert wird fuer nach der Schleife gemerkt, es ist der einzige != -1
					if ((a = l.schritt()) > b)
						b = a;
				}
				if (b != -1)
					zeichenLesen(b);

				//Falls mit dem letzten Zeichen ein EmE Befehl beendet wurde, wurde fertig() gecallt
				if (positionImLochstreifen == -1)
					return;

				zeitpunkt -= 0.1f;

				//Falls positionImLochstreifen >= gegebenerLochstreifen.Length, sind bereits alle Teile ausgegeben
				if (positionImLochstreifen < gegebenerLochstreifen.Length)
				{
					lochstreifen.Add(Instantiate(prefabs[gegebenerLochstreifen[positionImLochstreifen]], start, rotation).GetComponent<LochstreifenScript>());
					//Abrollen
					lochstreifenRolle.transform.Rotate(0, 0, -10);
				}

				positionImLochstreifen++;

				//Die letzte relevante Stelle ist abgelesen
				if (positionImLochstreifen >= gegebenerLochstreifen.Length + 25)
				{
					fertig();
				}
			}
		}
	}


	public override void reset ()
	{
		modus = Modus.Warten;
	}


	public override void schaltimpuls(int i)
	{
		modus = Modus.Schaltimpuls;
	}


	public override void bringen ()
	{
		if (peripherie.freigabe)
		{
	        if (outputBuffer.Count != 0)
			{
	            rechenwerk.bringenCallBack(outputBuffer[0]);
	            outputBuffer.RemoveAt(0);
			}
	        else
	        {
			  StartCoroutine(bringenVervollstaendigen());
			  modus = Modus.Bringtransfer;
	        }
		}
	}


	IEnumerator bringenVervollstaendigen ()
	{
		while (befehlsBuffer == "")
		{
			yield return null;
		}

		Wort ausgabe = fernschreiber.kodieren(befehlsBuffer.Substring(0,1));
		befehlsBuffer = befehlsBuffer.Substring(1);
		rechenwerk.bringenCallBack(ausgabe);

		modus = Modus.Warten;
	}


	//Beim Bringtransfer wird die Eingabe direkt als Text kodiert
	public override void bringTransfer(int anzahl)
	{
		if (peripherie.freigabe)
		{
			modus = Modus.Bringtransfer;
			//Eine Coroutine wird gestartet, da sonst der aktuelle Frame nicht beendet wird
			StartCoroutine(bringTransferVervollstaendigen(anzahl));
		}
	}


	IEnumerator bringTransferVervollstaendigen(int anzahl)
	{
		//Die Funktion wartet bis genug Woerter zusammen gekommen sind
		while (outputBuffer.Count < anzahl)
		{
			yield return null;
		}
		if (outputBuffer.Count == anzahl)
			rechenwerk.bringTransferCallBack(outputBuffer.ToArray());

		//Wenn zu viele Elemente im Buffer sind, muss genauer geschaut werden
		else
		{
			Wort[] ausgabe = new Wort[anzahl];
			for (int i = 0; i < anzahl; i++)
			{
				ausgabe[i] = outputBuffer[i];
			}

			rechenwerk.bringTransferCallBack(ausgabe);
		}
		modus = Modus.Warten;
	}


	public void neuerLochstreifen (int[] eingabe)
	{
		//Lochstreifen kann nur gewechselt werden, wenn kein Lochstreifen anliegt oder gerade nicht gelesen wird
		if (modus == Modus.Warten || positionImLochstreifen == -1)
		{
			//Eventuell noch vorhandener Lochstreifen wird verworfen
			fertig();

			gegebenerLochstreifen = eingabe;
			positionImLochstreifen = 0;

			lochstreifenRolle.SetActive(true);
			umklapp.SetActive(true);

			LochstreifenScript temp;
			Vector3 verschiebung = new Vector3 (0, 0, 0.254f);

			for (int i = 0; i <= 27; i++)
			{
				temp = Instantiate(prefabs[0], start + i*verschiebung, rotation).GetComponent<LochstreifenScript>();
				temp.phase = i;
				lochstreifen.Add(temp);
			}
		}
	}


	void fertig ()
	{
		audio.Stop();

		foreach (LochstreifenScript l in lochstreifen)
			l.deletos();

		lochstreifen.Clear();

		lochstreifenRolle.SetActive(false);
		umklapp.SetActive(false);

		positionImLochstreifen = -1;
	}


	//Hier ist groessenteils der Code von FernschreiberScript.schreiben uebernommen
	public void zeichenLesen (int wert)
	{
		char inhalt = fernschreiber.dekodieren(wert, buchstabenEingabe, out buchstabenEingabe);

		//befehlsBuffer wird gefuellt, bis ein Befehl vollstaendig ist oder ein Textblock zuende
        if (eingabeIstText)
        {
            //Textblock zuende
            if (inhalt == '\'' && einApostroph)
            {
                //Es ist wichtig das erst geparst wird, damit der Parser weiss, dass es sich um Text handelt
                befehlNachOutputBuffer(fernschreiber.textParser(befehlsBuffer));
                eingabeIstText = false;
            }
            //Erster Apostroph
            else if (inhalt == '\'')
                einApostroph = true;
            else
            {
                if (einApostroph)
                {
                    einApostroph = false;
                    //Da das vorige Apostroph nicht zum Ende der Texteingabe gehoerte, muss es nachgereicht werden
                    befehlsBuffer += '\'';
					if (++zeichenZaehler == 3)
					{
						befehlNachOutputBuffer(fernschreiber.textParser(befehlsBuffer));
						zeichenZaehler = 0;
					}
                }
                befehlsBuffer += inhalt;
                zeichenZaehler++;

                //Jeweils drei Zeichen werden zu einem Wort codiert
                if (zeichenZaehler == 3)
                {
                    befehlNachOutputBuffer(fernschreiber.textParser(befehlsBuffer));
                    zeichenZaehler = 0;
                }
            }
        }
        else
        {
            //Zwei ' hintereinander bedeuten, dass ein Texblock startet
            if (inhalt == '\'' && einApostroph)
            {
                //Der unfertige vorige Befehl wird verworfen
                befehlsBuffer = "";
                eingabeIstText = true;
                einApostroph = false;
            }
            else if (inhalt == '\'')
            {
                einApostroph = true;
            }
            else
            {
                //Falls ein Trennzeichen eingegeben wurde wird versucht den Befehl zu parsen
                if (inhalt == ' ' || inhalt == '<' || inhalt == '§')
				{
                    befehlNachOutputBuffer(fernschreiber.befehlsParser(befehlsBuffer));
				}
                else
                {
                    befehlsBuffer += inhalt;
                    if (einApostroph)
                        einApostroph = false;
                }
            }
        }
	}


	void befehlNachOutputBuffer(int[] inhalt)
	{
		befehlsBuffer = "";

		//Vorhergegangener U Bandbefehl wird abgearbeitet
		if (bandBefehlStehtNochAus && (inhalt[0] == -2 || inhalt[0] == -3) && modus == Modus.Schaltimpuls)
		{
			rechenwerk.bandBefehlInput(outputBuffer.ToArray(), bandBefehlZieladresse);
			outputBuffer.Clear();
			bandBefehlStehtNochAus = false;
		}

		switch (inhalt[0])
		{
			case -1:
				return;
			//U Bandbefehl
            case -2:
                bandBefehlZieladresse = inhalt[1];
                bandBefehlStehtNochAus = true;
                return;
			case -3:
				rechenwerk.bandBefehlSprung(inhalt[1]);
				modus = Modus.Warten;
				fertig();
				return;
            case -4:
                //Nichtstun, da nur unsichtbare Zeichen eingegeben wurden
                return;
			default:
				outputBuffer.Add(new Wort (inhalt[0]));
				if (inhalt[1] != -1)
					outputBuffer.Add(new Wort (inhalt[1]));
				return;
		}
	}


	void OnMouseDown ()
	{
		if(!EventSystem.current.IsPointerOverGameObject())
		{
			int[] eingabe = ui.getAktuellenLochstreifen();
			//Falls das UI keinen Lochstreifen hat, wird {-1} zurueckgegeben
			if (eingabe[0] != -1)
				neuerLochstreifen(eingabe);
		}
	}
}
