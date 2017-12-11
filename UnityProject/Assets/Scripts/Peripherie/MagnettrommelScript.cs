using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagnettrommelScript : PeripherieScript {

	public AudioHandlerScript audioHandler;

	//256 Spuren á 69 Woerter
	Wort[,] speicher = new Wort[256, 69];
	bool[,] parityBits = new bool[356, 69];

	bool _eingeschaltet = false;
	bool eingeschaltet
	{
		get {return _eingeschaltet;}
		set
		{
			if (_eingeschaltet && !value)
			{
				bereit = false;
				audioHandler.trommelStopp();
			}
			else if (!_eingeschaltet && value)
			{
				//Vorwahl Licht an
				lichter[6].SetActive(true);
				//Bis die Trommel bereit ist vergehen rund 120 Sekunden
				zeitBisEinschalten = 110f + UnityEngine.Random.Range(0, 20);
				audioHandler.trommelStarten();
			}

			_eingeschaltet = value;
			lichter[0].SetActive(value);
			lichter[1].SetActive(value);
		}
	}
	bool _bereit = false;
	new bool bereit
	{
		get {return _bereit;}
		set
		{
			_bereit = value;
			lichter[2].SetActive(value);
			lichter[3].SetActive(value);
			//Vorwahl Licht aus
			lichter[6].SetActive(false);
		}
	}

	float zeitBisEinschalten;


	//7 Stück 0,1 Trommel ein; 2,3 trommel fertrig; 4,5 trommel alarm; 6 ist vorwahl
	public GameObject[] lichter;


	void Start ()
	{
		//Speicher wird leer initialisiert
		for (int i = 0; i < speicher.GetLength(0); i++)
			for (int j = 0; j < speicher.GetLength(1); j++)
			{
				parityBits[i, j] = true;
			}
	}


	void Update ()
	{
		if (!bereit && eingeschaltet)
		{
			if (zeitBisEinschalten > 0)
				zeitBisEinschalten -= Time.deltaTime;

			if (zeitBisEinschalten <= 0)
			{
				bereit = true;
			}
		}
	}


	public override void maschineAus()
	{
		//Nichts tun, die Magnettrommel reagiert nicht auf ausschalten der Z25
	}


	public override void reset ()
	{
		//Speicher wird leer initialisiert
		for (int i = 0; i < speicher.GetLength(0); i++)
			for (int j = 0; j < speicher.GetLength(1); j++)
			{
				speicher[i, j] = 0;
				parityBits[i, j] = true;
			}
	}


	public void anAus (bool an)
	{
		if (!rechenwerk.getEinAus())
			eingeschaltet = an;
	}


	public override void umspeichertransfer (Wort[] inhalt)
	{
		if (bereit && peripherie.freigabe)
		{
			int spur = (int) speicherAdresse;
			if (spur >= 256)
				Debug.Log("SpurNr zu hoch in schreiben() MagnettrommelScript");

			int spurIndex = 0;

			for (int i = 0; i < inhalt.Length; i++)
			{
				speicher[spur, spurIndex] = inhalt[i];

				parityBits[spur, spurIndex] = inhalt[i].getParity();

				spurIndex++;

				//Automatischer Wechsel zur naechsten Spur
				if (spurIndex >= 69)
				{
					spur++;
					spurIndex = 0;

					if (spur >= 256)
					{
						//Im Handbuch ist dieser Fall nicht definiert. Um Pointerfehler zu vermeiden, wird hier einfach gewrappt.
						spur = 0;
					}
				}
			}

			peripherie.freigabe = false;
		}
	}


	public override void bringTransfer (int anzahl)
	{
		if (bereit && peripherie.freigabe)
		{
			int spur = (int) speicherAdresse;
			if (spur >= 256)
				Debug.Log("SpurNr zu hoch in lesen() MagnettrommelScript");

			Wort[] ausgabe = new Wort[anzahl];
			int i = 0;
			int spurIndex = 0;

			while (anzahl > 0)
			{
				if (speicher[spur, spurIndex].getParity() != parityBits[spur, spurIndex])
				{
					//Alarmbehandlung
					Debug.Log("Alarm in MagnettrommelScript");
				}

				ausgabe[i] = speicher[spur, spurIndex];

				i++;
				spurIndex++;
				anzahl--;

				//Automatischer Wechsel zur naechsten Spur
				if (spurIndex >= 69)
				{
					spur++;
					spurIndex = 0;

					if (spur >= 256)
					{
						//Im Handbuch ist dieser Fall nicht definiert
						spur = 0;
					}
				}
			}

			rechenwerk.bringTransferCallBack(ausgabe);

			peripherie.freigabe = false;
		}
	}


	public Wort[,] gesamtenInhaltAuslesen ()
	{
		return speicher;
	}


	public void gesamtenInhaltSchreiben (Wort[,] inhalt)
	{
		speicher = inhalt;

		for (int i = 0; i < speicher.GetLength(0); i++)
			for (int j = 0; j < speicher.GetLength(1); j++)
			{
				parityBits[i, j] = speicher[i, j].getParity();
			}
	}
}
