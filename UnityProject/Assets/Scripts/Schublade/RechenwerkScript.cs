using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Linq;


public class RechenwerkScript : MonoBehaviour
{
	[Tooltip("Referenz zu den einzelnen Knoepfen")]
	public KnopfScript[] knoepfe;				//Referenzen zu den Knopf Objekten

	public MagnettrommelScript magnetTrommel;

	public Text linkeSpeicherUebersicht;
	public Text rechteSpeicherUebersicht;
	public RectTransform textWrapperSpeicherUebersicht;

	public Text azrText;

	bool uebersichtAktiv = true;
	public bool speicherUebersichtAktiviert
	{
		get {return uebersichtAktiv;}
		set
		{
			uebersichtAktiv = value;
			speicheruebsichtUpdate();
		}
	}

	public Text letzteBefehleText;

	public AudioHandlerScript audioHandler;
	[Tooltip("0-3 entsprechen s7 g0-3, 4-7 entsprechen s8 g0-3 ect.")]
	public GameObject[] peripherie;

	public int geschwindigkeitsMultiplikator;

	public bool originalGeschwindigkeit {get; set;}
	bool strom = false;
	public bool stromAn
	{
		get {return strom;}
		set
		{
			knoepfe[21].setUnteresLicht(value);
			strom = value;
			if (!value)
			{
				maschineAus();
				nacht = false;
				knoepfe[23].setUnteresLicht(false);
			}
		}
	}

	public enum Darstellung
	{
		Binaer,
		Dezimal,
		Befehl
	};
	Darstellung darstellung = Darstellung.Binaer;
	public int speicherDarstellung
	{
		get {return (int) darstellung;}
		set
		{
			darstellung = (Darstellung) value;
			speicheruebsichtUpdate();
		}
	}

	Queue letzteBefehle = new Queue();

	bool letzterBefehlWarStop = false;
	//Entspricht genau dem Sprungbefehl Bitcode
	Wort sprungbefehlFallsStop = 31744;

	Speicher speicher = new Speicher(16384);
	int speicherLaenge;
	BitArray pult;						//Die Bits repraesentieren den unteren Bereich der Hauptknoepfe

	Wort b;								//Befehlsregister
	Wort osr;							//Operationssteuerregister (Stellen 10-15 von b)
	Wort m;								//Operationsparameter (Stellen 0-9 aus b)
	Wort asr;							//Adressensteuerregister (0-9 entsprechen m, 10-14 aus aer)
	Wort aer;							//Adressenerweiterungsregister
	Wort azr;							//Adressenzaehlregister (program counter), 15 Stellen genutzt
	bool zs = false;					//Zaehlerstandsregister
	bool bd = false;					//Bedingungsregister

	bool maschineGestoppt = true;
	bool maschineEingeschaltet = false;
	bool nacht = false;
	bool bedSchalter = false;
	bool programmUnterbr = false;

	bool wartenAufBringTransfer = false;

	int wortzeiten = 0;					//Die Variable wird benutzt um zu bestimmen wie lange gewartet werden muss
	int letzteSonderadresse = 0;		//Wird fuer Testsprung gebraucht

	const int z2 = 2;					//Stelle des Zaehlregisters Z2 im Speicher
	const int v = 3;					//Akkumulatorverlaengerung
	const int a = 4;					//Akkumulator
	const int ras = 5;					//Rueckkehradressenspeicher

	const int p = 17;					//Stelle von Bedingung P im Wort
	const int q = 16;					//Bedingung Q
	const int g = 15;					//Adressmodifikations-Bit G
	const int vorzeichen = 17;			//Vorzeichenstelle des Wortes
	const int doppelwortVorzeichen = 35;


	void Start ()
	{
		speicherLaenge = speicher.Length;

		pult = new BitArray(18, false);

		if (knoepfe.Length < 25)
			Debug.Log("Nicht alle Knoepfe in Rechenwerk zugewiesen");

		if (linkeSpeicherUebersicht == null || rechteSpeicherUebersicht == null)
			Debug.Log("SpeicherUebersicht Text in Rechenwerk nicht zugewiesen");

		if (audioHandler == null)
			Debug.Log("AudioHandler in Rechenwerk nicht zugewiesen");

		originalGeschwindigkeit = true;
		stromAn = true;

		reset();
	}


	void Update ()
	{
		if (speicher.wasUpdated())
			speicheruebsichtUpdate();

		if (maschineEingeschaltet)
		{
			if (!maschineGestoppt)
			{
				knoepfe[20].setOberesLicht(true);

				//Anzahl der Wortzeiten die seit dem letzten Update haetten ausgefuehrt werden koennen
				//Die Zuse Z25 schafft pro Sekunde etwa 11700 Wortzeiten
				int moeglicheWortzeiten = (int) ( 11700 * (originalGeschwindigkeit ? 1 : geschwindigkeitsMultiplikator) * Time.deltaTime);

				while (wortzeiten <= moeglicheWortzeiten && !maschineGestoppt)
				{
					naechsterBefehl();
				}

				wortzeiten -= moeglicheWortzeiten;
			}
		}


		if (letzterBefehlWarStop)
		{
			letzterBefehlWarStop = false;
			b = sprungbefehlFallsStop;
			hauptknoepfeLampenAktualisieren();
		}

		//AZRText wird immer aktualisiert
		azrText.text = "AZR: " + (int) azr;
	}








	//########## BEFEHLE #########








	//Km, Befehl 0
	void keineOperation ()
	{
		//Sonderfall PKm, nach Handbuch
		if (b[p] && !b[q])
			bd = speicher[asr][vorzeichen];			//17te Stelle in Zelle <asr>

		//Befehl dauert eine Wortzeit
		wortzeiten++;
	}


	//Am, Befehl 1
	void addition ()
	{
		speicher[a] += speicher[asr];

		//Befehl dauert eine Wortzeit
		wortzeiten++;
	}


	//AAm, Befehl 2
	void doppelwortAddition ()
	{
		//Achtung, im Akkumulator ist m-1 das kleinere Wort. Sonst ist es m
		Doppelwort akku = new Doppelwort(speicher[v], speicher[a]);
		Doppelwort speicherM = new Doppelwort(speicher[asr], speicher[asr - 1]);
		akku += speicherM;

		speicher[v] = akku.getUnteresWort();
		speicher[a] = akku.getOberesWort();

		//Befehl dauert zwei Wortzeiten
		wortzeiten += 2;
	}


	//Bm, Befehl 3
	void bringen ()
	{
		speicher[a] = speicher[asr];

		//Falls die angesprochene Adresse eine Sonderadresse mit angeschlossener Peripherie ist
		if (asr >= 7 && asr <= 24)
			if (peripherie[(asr - 7) * 4] != null)
			{
				peripherie[(asr - 7) * 4].SendMessage("bringen");
				wartenAufBringTransfer = true;
			}
		//Befehl dauert eine Wortzeit
		wortzeiten++;
	}


	public void bringenCallBack (Wort wort)
	{
		speicher[a] = wort;
		wartenAufBringTransfer = false;
	}


	//BBm, Befehl 4
	void doppelwortBringen ()
	{
		speicher[v] = speicher[asr];
		speicher[a] = speicher[asr - 1];

		//Befehl dauert zwei Wortzeiten
		wortzeiten += 2;
	}


	//Im, Befehl 5
	void konjunktion ()
	{
		speicher[a] &= speicher[asr];

		//Befehl dauert eine Wortzeit
		wortzeiten++;
	}


	//DIm, Befehl 6
	void disjunktion ()
	{
		speicher[a] |= speicher[asr];

		//Befehl dauert eine Wortzeit
		wortzeiten++;
	}


	//CAm, Befehl 7
	void konstantenAddition ()
	{
		speicher[a] += m;

		//Befehl dauert eine Wortzeit
		wortzeiten++;
	}


	//CBm, Befehl 8
	void konstantenBringen ()
	{
		speicher[a] = m;

		//Befehl dauert eine Wortzeit
		wortzeiten++;
	}


	//CIm, Befehl 9
	void konstantenKonjunktion ()
	{
		speicher[a] &= m;

		//Befehl dauert eine Wortzeit
		wortzeiten++;
	}


	//CSm, Befehl 10
	void konstantenSubtraktion ()
	{
		speicher[a] -= m;

		//Befehl dauert eine Wortzeit
		wortzeiten++;
	}


	//CTm, Befehl 11
	void konjunktionMitNegativerKonstante ()
	{
		speicher[a] &= -m;

		//Befehl dauert eine Wortzeit
		wortzeiten++;
	}


	//ISm, Befehl 12
	void konjuntionMitNegativemOperand ()
	{
		speicher[a] &= -speicher[asr];

		//Befehl dauert eine Wortzeit
		wortzeiten++;
	}


	//NSm, Befehl 13
	void negativBringen ()
	{
		speicher[a] = -speicher[asr];

		//Befehl dauert eine Wortzeit
		wortzeiten++;
	}


	//Sm, Befehl 14
	void subtraktion ()
	{
		speicher[a] -= speicher[asr];

		//Befehl dauert eine Wortzeit
		wortzeiten++;
	}


	//SSm, Befehl 15
	void doppelwortSubtraktion ()
	{
		//Achtung, im Akkumulator ist m-1 das kleinere Wort. Sonst ist es m
		Doppelwort akku = new Doppelwort(speicher[v], speicher[a]);
		Doppelwort speicherM = new Doppelwort(speicher[asr], speicher[asr - 1]);

		akku -= speicherM;

		speicher[v] = akku.getUnteresWort();
		speicher[a] = akku.getOberesWort();

		//Befehl dauert zwei Wortzeiten
		wortzeiten += 2;
	}


	//Um, Befehl 16
	void umspeichern ()
	{
		//Die Speicheradressen 0, 1 und 6 koennen im Original nicht ueberschreiben werden
		if (asr != 0 && asr != 1 && asr != 6)
			speicher[asr] = speicher[a];

		//Falls das Ziel eine Sonderadresse mit angeschlossener Peripherie ist
		if (asr >= 7 && asr <= 24)
			if (peripherie[(asr - 7) * 4] != null)
			{
				//Speicheradresse selber bleibt leer. Das Wort wird in die Peripherie uebertragen
				speicher[asr] = 0;
				peripherie[(asr - 7) * 4].SendMessage("umspeichern", speicher[a]);
			}

		//Befehl dauert eine Wortzeit
		wortzeiten++;
	}


	//UUm, Befehl 17
	void doppelwortUmspeichern ()
	{
		//Achtung, im Akkumulator ist m-1 das kleinere Wort. Sonst ist es m

		//Die Speicheradressen 0, 1 und 6 koennen im Original nicht ueberschreiben werden
		if (asr >= 2 && asr != 6)
			speicher[asr] = speicher[v];

		if ((asr - 1) >= 2 && (asr - 1) != 6)
			speicher[asr - 1] = speicher[a];

		//Befehl dauert zwei Wortzeiten
		wortzeiten += 2;
	}


	//Mm, Befehl 18
	void multiplikation ()
	{
		//Multiplikator steht im Akkumulator, Multipikand steht in m
		//Adressen kleiner 32 nicht zugelassen
		if (asr >= 32)
		{
			int i = 19;
			int wertZ2 = (int)speicher[z2].getRange(0, 10);
			//Verkuerzte Multiplikation
			if (zs)
				i = wertZ2;

			//Hier werden Doppelwoerter benutzt um den Programmcode zu vereinfachen.
			//Beide Werte sind eigentlich einzel Wort.
			Doppelwort akku, multiplikand;

			//Falls Multiplikator negativ, beide Operatoren umkehren. Multiplikand darf negativ sein
			//Multipikand wird als Doppelwort zwischengespeichert, um leichter addieren zu koennen
			if (speicher[a][vorzeichen])
			{
				akku = new Doppelwort(-speicher[a], 0);
				multiplikand = new Doppelwort(0, -speicher[asr]);
			}
			else
			{
				akku = new Doppelwort(speicher[a], 0);
				multiplikand = new Doppelwort(0, speicher[asr]);
			}

			bool hierAddieren = false;

			for (; i > 0; i--)
			{
				hierAddieren = akku[0] ? true : false;

				akku >>= 1;

				if (hierAddieren)
					akku += multiplikand;
			}

			//Falls eine verkuerzte Multiplikation wurden weniger Wortzeiten benoetigt
			if (zs)
			{
				wortzeiten -= wertZ2;
			}

			speicher[v] = akku.getUnteresWort();
			speicher[a] = akku.getOberesWort();

			//Korrekten Endzustand des Zaehlregisters herstellen
			speicher[z2] = 0;
			zs = false;

			//Ausfuehrung in voller Laenge benoetigt 21 Wortzeiten
			wortzeiten += 21;
		}
	}


	//Dm, Befehl 19
	void division ()
	{
		//Dividend als Doppelwort im Akku, Divisor an Stelle m
		//Adressen kleiner 32 nicht zugelassen
		if (asr >= 32)
		{
			int i = 19;
			int wertZ2 = (int)speicher[z2].getRange(0, 10);


			//Der Divisor wird der Einfachheit halber auch als Doppelwort implementiert
			Doppelwort akku = new Doppelwort(speicher[v], speicher[a]);
			Doppelwort divisor = new Doppelwort(0, speicher[asr]);

			//Akkuinhalt wird im verkuertzen Fall noch gebraucht
			Wort akkuZwischenSpeicher = speicher[a];

			//Das Vorzeichen des Ergebnis muss zwischengespeichert werden, da nur mit positiven Zahlen gerechnet wird
			bool ergebnisVorzeichen = false;
			//Dividend negativ
			if (akku[doppelwortVorzeichen])
			{
				akku = -akku;
				ergebnisVorzeichen = true;
			}

			//Divisor negativ
			if (divisor[doppelwortVorzeichen])
			{
				divisor = new Doppelwort(0, -speicher[asr]);
				ergebnisVorzeichen = !ergebnisVorzeichen;
			}


			//Verkuerzte Division, es wird vorab geshifted und spaeter entsprechend seltener subtrahiert
			if (zs)
			{
				i = wertZ2;
				akku <<= 19 - wertZ2;

				//Das Verkuerzen hat in erster Linie den Zweck die Zahl der noetigen Wortzeiten zu verringern
				wortzeiten -= wertZ2;
			}


			//Es wird immer dann addiert, wenn im vorigen Schritt ein Ueberlauf passiert ist
			bool hierAddieren = false;

			for (; i > 1; i--)
			{
				akku <<= 1;
				if (hierAddieren)
					akku += divisor;
				else
					akku -= divisor;

				//Ein Ueberlauf hat stattgefunden
				if (akku[doppelwortVorzeichen])
					hierAddieren = true;
				else
				{
					hierAddieren = false;
					//Wenn der Divisor in den verbliebenen Dividenden passt, wird eine 1 nachgeschoben
					akku += 1;
				}
			}

			//Falls ein Rest bleibt, wurde einmal zu oft subtrahiert. Das wird hier rueckgaengig gemacht
			if (hierAddieren)
				akku += divisor;

			//Oberes und unteres Wort werden vertauscht gespeichert, damit das Ergebnis in a steht
			//Vorzeichen korrigiert
			if (ergebnisVorzeichen)
				speicher[a] = -akku.getUnteresWort();
			else
				speicher[a] = akku.getUnteresWort();


			//Im verkuertzen Fall wurde der urspruengliche Akkuinhalt nicht ganz rausgeschoben
			//Da hier nicht sogerechnet wird, wie im Original, muss das korrigiert werden
			if (zs)
			{
				Wort temp = speicher[a];
				temp[wertZ2 - 1] = !ergebnisVorzeichen;

				akkuZwischenSpeicher = akkuZwischenSpeicher << (wertZ2 + 1);
				speicher[a] = temp | akkuZwischenSpeicher;
			}


			//Im Original wird, aus mir unbekannten Gruenden, ein zusaetzlicher Rechenschritt durchgefuehrt
			//Die daraus resultierende Verfaelschung des Restes wird hier vorgenommen
			akku <<= 1;
			akku -= divisor;
			speicher[v] = akku.getOberesWort();
			speicher[v] = speicher[v] + 1;


			//Korrekten Endzustand des Zaehlregisters herstellen
			speicher[z2] = 0;
			zs = false;

			//Die volle Laenge benoetigt 22 Wortzeiten
			wortzeiten += 22;
		}
	}


	//SH, Befehl 20
	void verschiebeBefehl ()
	{
		int distanz = (int) m.getRange(0, 5);

		Doppelwort akku = new Doppelwort(speicher[v], speicher[a]);

		//Es werden einzeln die Bits von m ausgelesen um zwischen den Modi zu unterscheiden
		//Linksverschiebung
		if (m[9] && !m[8])
		{
			//Bei Linksverschiebung wird um m+1 verschoben
			distanz++;

			//Verkoppelte Verschiebung
			if (m[7])
			{
				for (int i = 0; i < distanz; i++)
					akku <<= 1;

				speicher[v] = akku.getUnteresWort();
				speicher[a] = akku.getOberesWort();
			}
			else
			{
				for (int i = 0; i < distanz; i++)
					speicher[a] <<= 1;
			}

			//Bei der Linksverschiebung fallen zusaetzliche Wortzeiten an
			wortzeiten += distanz;
		}
		//Rechtsverschiebung
		else if (m[8] && !m[9])
		{
			//Originalgeteruen Endzustand des Zaehlregisters herstellen
			speicher[z2] = 0;
			zs = false;

			//Schreibe Verschiebungsueberschuss in Zaehler z2
			if (distanz > 18)
			{
				speicher[z2] = distanz - 18;
				distanz = 18;
			}
			//Verkoppelte Verschiebung
			if (m[7])
			{
				//Zyklische Verschiebung
				if (m[6])
				{
					bool ueberschuss = false;
					for (int i = 0; i < distanz; i++)
					{
						ueberschuss = akku[0];
						akku >>= 1;
						akku[35] = ueberschuss;
					}
				}
				else
				{
					bool vorzeichenWert = akku[doppelwortVorzeichen];
					for (int i = 0; i < distanz; i++)
					{
						akku >>= 1;
						akku[doppelwortVorzeichen] = vorzeichenWert;
					}
				}

				speicher[v] = akku.getUnteresWort();
				speicher[a] = akku.getOberesWort();
			}
			else
			{
				Wort akkuInhalt = speicher[a];
				//Zyklische Verschiebung
				if (m[6])
				{
					bool ueberschuss = false;
					for (int i = 0; i < distanz; i++)
					{
						ueberschuss = akkuInhalt[0];
						akkuInhalt >>= 1;
						akkuInhalt[vorzeichen] = ueberschuss;
					}
				}
				else
				{
					bool vorzeichenWert = akkuInhalt[vorzeichen];
					for (int i = 0; i < distanz; i++)
					{
						akkuInhalt >>= 1;
						akkuInhalt[vorzeichen] = vorzeichenWert;
					}
				}
				speicher[a] = akkuInhalt;
			}
		}

		//Befehl dauert mindestens eine Wortzeit
		wortzeiten++;
	}


	//TIm, Befehl 21
	void tauscheInhalt ()
	{
		//Adressen kleiner 32 nicht zugelassen
		if (m >= 32)
		{
			//Inhalt von m wird zwischengespeichert
			Wort stelleM = speicher[m];

			//Ueberschreibe <m> mit dem Inhalt von aer
			speicher[m] = aer.getRange(10, 14) >> 10;

			//Ueberschreibe aer 10-14 mit Stllen 0-4 von m
			aer = stelleM.getRange(0,4) << 10;
		}

		//Befehl dauert eine Wortzeit
		wortzeiten++;
	}


	//UTm, Befehl 22
	void umspeichertransfer ()
	{
		if (letzteSonderadresse >= 0 && letzteSonderadresse < peripherie.Length && peripherie[letzteSonderadresse] != null)
		{
			//t+1 Woerter sollen gespeichert werden
			int anzahl = (int) speicher[z2] + 1;
			Wort[] ausgabe = new Wort[anzahl];

			for (int i = 0; i < anzahl; i++)
			{
				ausgabe[i] = speicher[asr + i];
			}

			//Es wird immer an die durch "Freigabeaktivierung" aktivierte Adresse gesendet
			//Vor dem Umspeichertransfer wurde vielleicht die gewuenschte Zieladresse an speicher[s] gespeichert.
			peripherie[letzteSonderadresse].SendMessage("umspeichertransfer", ausgabe);
		}

		wortzeiten += (int) speicher[z2] + 1;

		//Originalgeteruen Endzustand des Zaehlregisters herstellen
		speicher[z2] = 0;
		zs = false;
	}


	//BTm, Befehl 23
	void bringTransfer ()
	{
		if (letzteSonderadresse >= 0 && letzteSonderadresse < peripherie.Length && peripherie[letzteSonderadresse] != null)
		{
			int anzahl = (int) speicher[z2] + 1;

			wartenAufBringTransfer = true;

			//Es wird immer an die durch "Freigabeaktivierung" aktivierte Adresse gesendet
			//Vor dem Umspeichertransfer wurde vielleicht die gewuenschte Zieladresse an speicher[s] gespeichert.
			peripherie[letzteSonderadresse].SendMessage("bringTransfer", anzahl);
		}

		wortzeiten += (int) speicher[z2] + 1;

		//Originalgetereuen Endzustand des Zaehlregisters herstellen
		speicher[z2] = 0;
		zs = false;
	}


	//Wird von Peripherie bentutzt um angeforderte Daten zu uebergeben
	public void bringTransferCallBack (Wort[] inhalt)
	{
		for (int i = 0; i < inhalt.Length; i++)
		{
			speicher[asr + i] = inhalt[i];
		}

		wartenAufBringTransfer = false;
	}


	//Eingabe durch Bandbefehl
	public void bandBefehlInput (Wort[] inhalt, int ziel)
	{
		for (int i = 0; i < inhalt.Length; i++)
		{
			speicher[ziel + i] = inhalt[i];
		}
	}


	public void bandBefehlSprung (Wort ziel)
	{
		azr = ziel;
		b = speicher[azr];
		//Falls Maschine gestoppt war, startet der Bandbefehl
		stopZustand(false);
		wartenAufBringTransfer = false;
	}


	//Hm, Befehl 24
	void adressenerweiterungsregisterSetzen ()
	{
		aer = m.getRange(0,4) << 10;

		//Befehl dauert eine Wortzeit
		wortzeiten++;
	}


	//MB, Befehl 25
	void magnetbandoperation ()
	{
		//Falls ein Magnetband spaeter implementiert wird, kann hier der entsprechende Code eingefuegt werden
	}


	//Y(g+s) und X(g+s), Befehl 26
	void schaltimpulsOderFreigabeaktivierung ()
	{
		//Berechnung welches Peripheriegeraet angesprochen werden Sondefall
		//s ist die Sonderadresse (7-31, Stellen 0-4), g ist die Geraetadresse (0-3, Stellen 5-6)

		//Die Sonderadresse
		int s = (int) b.getRange(0, 4);

		//Der Geraetadressen Teil
		int g = (int) b.getRange(5, 6);
		g >>= 5;

		//Die Sonderadressen 7-31 mit Geraeteadresse 0 sind hier mit 0, 4, 8 etc. nummeriert
		//Auf 1-3, 5-7 ect. liegen die Geraeteadressen 1-3 der jeweiligen Sonderadresse
		s -= 7;
		s *= 4;
		s += g;
		//s ist jetzt der korrekte Index des Array "peripherie"


		//Stelle 9 (Wert 512) differenziert die beiden Befehle
		if (b[9])
		{
			//Befehl Y(g+s): Schaltimpuls Zur Externen Einheit
			//Ein Schaltimpuls der Laenge <z2> wird an externes Geraet gesendet
			int schaltimpulsLaenge = (int) speicher[z2];

			//Wenn an der angegebenen Adresse kein Geraet anliegt, passiert nichts
			if (s >= 0 && s < peripherie.Length && peripherie[s] != null)
			{
				peripherie[s].SendMessage("schaltimpuls", schaltimpulsLaenge);
			}
			//<z2>+1 Wortzeiten benoetigt
			wortzeiten += schaltimpulsLaenge + 1;
			//Originalgeteruen Endzustand des Zaehlregisters herstellen
			speicher[z2] = 0;
			zs = false;
		}
		else
		{
			//Befehl X(g+s): Freigabeaktivierung
			//Freigabeaktivierung an Geraet wird angefordert, kann danach mit "Testsprung" überprueft werden
			if (s >= 0 && s < peripherie.Length && peripherie[s] != null)
				peripherie[s].SendMessage("freigabeaktivierung");

			//Adresse wird fuer "Testsprung", "Umspeichertransfer" oder "Bringtransfer" gemerkt
			letzteSonderadresse = s;

			//Eine Wortzeit benoetigt
			wortzeiten++;
		}
	}


	//Zm, Befehl 27
	void zaehlerladen ()
	{
		speicher[z2] = m;
		zs = true;

		//Befehl dauert eine Wortzeit
		wortzeiten++;
	}


	//STm, Befehl 28
	void stop ()
	{
		//Der stop Befehl laedt zusaetzlich einen Sprung
		letzterBefehlWarStop = true;
		for (int i = 0; i < 10; i++)
		{
			sprungbefehlFallsStop.setStelle(i, m.getStelle(i));
		}
		//Ob G uebernommen wird, geht aus Handbuch nicht hervor. Hier wird es gemacht.
		sprungbefehlFallsStop.setStelle(g, b[g]);

		stopZustand(true);
	}


	//Tm, Befehl 29
	void testsprung ()
	{
		bool freigabeLiegtVor = false;

		if (letzteSonderadresse >= 0 && letzteSonderadresse < peripherie.Length && peripherie[letzteSonderadresse] != null)
			freigabeLiegtVor = peripherie[letzteSonderadresse].GetComponent<PeripherieDatenScript>().freigabe;

		if (!freigabeLiegtVor)
			sprungbefehl();
	}


	//Fm, Befehl 30
	void sprungMitNotierung ()
	{
		speicher[ras] = azr;
		sprungbefehl();
	}


	//Em, Befehl 31
	void sprungbefehl ()
	{
		//Program Counter wird durch Parameter ueberschrieben
		//Sondefall E5, Rueckkehr zum Oberprogramm
		if (asr == 5)
		{
			//Statt nach m wird zur Rueckkehradresse gesprungen
			azr = speicher[ras];

			//aer und der Befehlsteil von ras werden dabei veraendert
			aer = speicher[ras].getRange(10,14);

			speicher[ras] = 0x03C00;
			speicher[ras] |= azr.getRange(0,9);
			//in ras(10,14) steht 31
		}
		//Sprung wird nur ausgefuehrt, wenn die angepeilte Adresse existiert
		else if (asr < speicherLaenge)
			azr = asr;

		//Befehl dauert eine Wortzeit
		wortzeiten++;
	}






	//########## RECHENPROZESS-FUNKTIONEN ##########






	//Entschluesselt b, fuehrt den Befehl aus (falls Bedingungen erfuellt) und geht zum naechsten Befehl ueber
	void naechsterBefehl ()
	{
		if (wartenAufBringTransfer)
		{
			wortzeiten++;
			return;
		}
		registerAktualisieren ();


		//Bedingungen ueberpruefen
		bool bedingungErfuellt = true;

		//Fall PQ
		if (b[p] && b[q])
		{
			if ((int)speicher[a] != 0)
				bedingungErfuellt = false;
		}

		//Fall P oder Fall Q
		else if ( (b[p] && !bd) || (b[q] && !speicher[a][vorzeichen]) )
			bedingungErfuellt = false;


		if (bedingungErfuellt)
			befehlAusfuehren ();
		//Keine Operation oder Sonderfall PKm
		else
			keineOperation();


		//Naechster Befehl in b geladen
		b = speicher[azr];

		if (maschineGestoppt)
			hauptknoepfeLampenAktualisieren();

		if(speicher.getAlarm())
		{
			//Alarm setzen
			knoepfe[23].setOberesLicht(true);
			stopZustand(true);
		}
	}


	void registerAktualisieren ()
	{
		//Befehl wird fuer Uebersicht gespeichert
		letzteBefehle.Enqueue(b);
		letzteBefehleUpdate();

		//osr bilden
		osr = b.getRange(10, 15) >> 10;

		//asr und Parameter m sind zunaechst gleich
		asr = b.getRange(0, 9);
		m = asr;

		//Adressenmodifikation
		if (b[g])
		{
			Wort vorigerBefehl = (azr > 0 ? speicher[azr - 1] : 0);
			//Adresserweiterung
			if (vorigerBefehl.getRange(10, 14) > 0)			//vorigerBefehl ist nicht Km, siehe Sonderfall
				asr |= aer;

			//Adressensubstitution
			else if (vorigerBefehl.getRange(15,17) == 0 && m >= 32)	//Bedingungen des vorigen Befehls sind 0
			{
				//Siehe Tabelle 4.2.1 in "Datenverarbeitungssystem Zuse Z25 Beschreibung"

				Wort index = m;
				//Das t aus vorhergegangenem Kt
				Wort t = vorigerBefehl.getRange(0,9);
				//<m>
				Wort indexStelle;
				//m~
				Wort mTilde;

				//Es wird so oft wie noetig substituiert um das Finale m~ zu erhalten
				do
				{
					if (index >= 32)
						indexStelle = speicher[index];
					else
						indexStelle = 0;

					mTilde = indexStelle + t;
					mTilde = mTilde.getRange(0,14);

					//Falls Stelle 17 gesetzt ist, wird mit aer erweitert
					if (indexStelle[17])
						mTilde = mTilde.getRange(0,9) | aer;

					//Sonst wird das aer selbst ueberschrieben, und das nur wenn nicht der in Tabelle 4.2.2 beschriebene Sonderfall
					//Ktm, GEm mit Index zwischen 32 und 63 vorkommt
					else
					{
						if ( !( (vorigerBefehl.getRange(10,14) == (31 << 10))
								&& index >= 32 && index <= 63) )
						{
							aer = mTilde.getRange(10,14);
						}
					}

					//ModSchluessel Stelle 16
					if (indexStelle[16])
						speicher[index] = speicher[index] + t;

					//Falls mehrfach substituiert wird, ist mTilde die Stelle der neuen Indexzelle
					index = mTilde;
					//Falls mehrfach substituiert wird, wird t 0
					t = 0;
				} while (indexStelle[15]);

				//Zuletzt wird der resultierende Befehl in die noetigen Register geschrieben
				asr = mTilde.getRange(0,14);
				m = mTilde.getRange(0,10);
				//osr bleibt unveraendert

				//Die Substitution benoetigt eine Wortzeit
				wortzeiten++;
			}
		}

		//azr um eins erhoehen
		azr += 1;

		if ((int) azr >= speicherLaenge)
			azr = 0;
	}


	public void reset()
	{
		maschineAus();

		//Der Speicher wird auch genullt
		for (int i = 0; i < speicherLaenge; i++)
		{
			speicher[i] = 0;
		}
		//An Speicherzelle 1 steht Konstant 2^17
		speicher[1] = 0x20000;

		letzteBefehle.Clear();
		letzteBefehleUpdate();
	}


	void stopZustand(bool neu)
	{
		maschineGestoppt = neu;
		knoepfe[18].setOberesLicht(neu);

		//Falls die AlarmNacht Taste gerastet ist, schaltet die Maschine beim nächsten Stop aus
		if(neu && nacht && maschineEingeschaltet)
			maschineAus();

		//Wenn Maschine nicht mehr gestoppt ist, wird  Alarm, falls vorhanden, ausgeschaltet
		if (!neu)
			knoepfe[23].setOberesLicht(false);

		if (neu)
		{
			hauptknoepfeLampenAktualisieren();

			//Knopf start
			knoepfe[20].setOberesLicht(false);

			wortzeiten = 0;
		}
	}


	void hauptknoepfeLampenAktualisieren ()
	{
		//Die Hauptknoepfe zeigen auf den oberen Lampen den Inhalt von b
		for (int i = 0; i <= 17; i++)
			knoepfe[i].setOberesLicht(b[i]);
	}


	void maschineAus ()
	{
		b = new Wort(0);
		osr = new Wort(0);
		m = new Wort(0);
		asr = new Wort(0);
		aer = new Wort(0);
		azr = new Wort(0);
		zs = false;
		bd = false;

		maschineEingeschaltet = false;
		wartenAufBringTransfer = false;
		stopZustand(true);
		bedSchalter = false;
		speicher[6] = 0;

		wortzeiten = 0;

		pult = new BitArray(18, false);
		for (int i = 0; i <= 24; i++)
		{
			knoepfe[i].setOberesLicht(false);
			knoepfe[i].setUnteresLicht(false);
		}

		//Laut Handbuch kann aus Nacht Zustand erst nach erneutem betätigen der AlarmNacht Taste gestartet werden.
		//Ich gehe vorerst davon aus, dass die Taste also auch im ausgeschalteten Zustand aktiv bleibt.
		if (nacht)
			knoepfe[23].setUnteresLicht(true);

		knoepfe[21].setUnteresLicht(stromAn);

		audioHandler.maschineStoppt();

		for (int i = 0; i < peripherie.Length; i++)
		{
			if (peripherie[i] != null)
				peripherie[i].SendMessage("maschineAus");
		}
	}


	//Entschluesselt osr und fuehrt den entsprechenden Befehl aus
	void befehlAusfuehren ()
	{
		//Stelle 5 (G) wird nicht gebraucht
		switch (osr.getRange(0,4))
		{
			case 0:
				keineOperation();
				break;
			case 1:
				addition();
				break;
			case 2:
				doppelwortAddition();
				break;
			case 3:
				bringen();
				break;
			case 4:
				doppelwortBringen();
				break;
			case 5:
				konjunktion();
				break;
			case 6:
				disjunktion();
				break;
			case 7:
				konstantenAddition();
				break;
			case 8:
				konstantenBringen();
				break;
			case 9:
				konstantenKonjunktion();
				break;
			case 10:
				konstantenSubtraktion();
				break;
			case 11:
				konjunktionMitNegativerKonstante();
				break;
			case 12:
				konjuntionMitNegativemOperand();
				break;
			case 13:
				negativBringen();
				break;
			case 14:
				subtraktion();
				break;
			case 15:
				doppelwortSubtraktion();
				break;
			case 16:
				umspeichern();
				break;
			case 17:
				doppelwortUmspeichern();
				break;
			case 18:
				multiplikation();
				break;
			case 19:
				division();
				break;
			case 20:
				verschiebeBefehl();
				break;
			case 21:
				tauscheInhalt();
				break;
			case 22:
				umspeichertransfer();
				break;
			case 23:
				bringTransfer();
				break;
			case 24:
				adressenerweiterungsregisterSetzen();
				break;
			case 25:
				magnetbandoperation();
				break;
			case 26:
				schaltimpulsOderFreigabeaktivierung();
				break;
			case 27:
				zaehlerladen();
				break;
			case 28:
				stop();
				break;
			case 29:
				testsprung();
				break;
			case 30:
				sprungMitNotierung();
				break;
			case 31:
				sprungbefehl();
				break;
		}
	}


	/*
	Die Knoepfe sind in folgender Weise nummeriert:
	0-17 die Hauptknoepfe
	18 StopWeiter
	19 Befehlsuebernahme
	20 Start
	21 MaschEin NetzEin
	22 ProgrammUnterbrFrei
	23 AlarmNacht
	24 BedSchalter
	25 TrommelEinAus
	26 TrommelFertig
	27 TrommelAlarm
	*/


	//Ein Knopf ruft knopfCall anonym, Rechenwerk sucht daraufhin den gedrueckten Knopf
	public void knopfCall(int gedrueckterKnopf)
	{
		//Die maschEinNetzEin Taste wird gesondert abgehandelt, da sie auch im ausgeschalteten Zustand funktionieren muss
		if (gedrueckterKnopf == 21)
			maschEinNetzEinKnopf();
		else if (maschineEingeschaltet)
		{
			//gedrueckterKnopf haelt die Nr des gedrueckten Knopf
			switch(gedrueckterKnopf)
			{
				case -1:
					Debug.Log("gedrueckten Knopf nicht richtig initialisiert");
					break;
				case 18:
					stopWeiterKnopf();
					break;
				case 19:
					befehlsuebernahmeKnopf();
					break;
				case 20:
					startKnopf();
					break;
				case 22:
					programmUnterbrFreiKnopf();
					break;
				case 23:
					alarmNachtKnopf();
					break;
				case 24:
					bedSchalterKnopf();
					break;
			}
			if (gedrueckterKnopf <= 17 && gedrueckterKnopf >= 0)
				hauptKnopf(gedrueckterKnopf);
		}
		//Falls Maschine durch Nacht Taste ausgeschaltet wurde muss diese noch funktionieren
		else if (nacht && gedrueckterKnopf == 23)
			alarmNachtKnopf();
	}

	//Die Knoepfe des Eingabewortes. Nummeriert 0-17
	void hauptKnopf(int nr)
	{
		pult[nr] = !pult[nr];
		knoepfe[nr].setUnteresLicht(pult[nr]);
	}


	//Nr 18
	void stopWeiterKnopf()
	{
		naechsterBefehl();
		stopZustand(true);
	}

	//Nr 19
	void befehlsuebernahmeKnopf()
	{
		stopZustand(true);
		for (int i = 0; i <= 17; i++)
			b[i] = pult[i];
		hauptknoepfeLampenAktualisieren();

		wartenAufBringTransfer = false;

		//Peripheriegeraeteingabe wird abgebrochen
		for (int i = 0; i < peripherie.Length; i++)
		{
			if (peripherie[i] != null)
				peripherie[i].SendMessage("maschineAus");
		}
	}


	//Nr 20
	void startKnopf()
	{
		stopZustand(false);
	}


	//Nr 21
	void maschEinNetzEinKnopf()
	{
		if (maschineEingeschaltet)
		{
			//Maschine wird ausgeschaltet indem zurueckgesetzt wird
			maschineAus();
		}
		else
		{
			//Die AlarmNacht Taste muss ausgeschaltet sein bevor eingeschaltet werden kann.
			if (!nacht && strom)
			{
				maschineEingeschaltet = true;
				stopZustand(true);
				knoepfe[21].setOberesLicht(true);
				audioHandler.maschineStartet();
			}
		}
	}


	//Nr 22
	void programmUnterbrFreiKnopf()
	{
		programmUnterbr = !programmUnterbr;
		knoepfe[22].setOberesLicht(programmUnterbr);
		//Im EMulator ist kein Peripheriegeraet implementiert, das die Programmunterbrechung benutzte, deshalb ist sie nicht implementiert
	}


	//Nr 23
	void alarmNachtKnopf()
	{
		nacht = !nacht;
		knoepfe[23].setUnteresLicht(nacht);
	}


	//Nr 24
	void bedSchalterKnopf()
	{
		//Liefert entweder 0 oder 1 in Speicherzelle 6
		bedSchalter = !bedSchalter;
		speicher[6] = (bedSchalter ? 1 : 0);
		knoepfe[24].setOberesLicht(bedSchalter);
	}







//########### Ab hier, Code fuer das User Interface ###############






	void speicheruebsichtUpdate ()
	{
		//Abbruch falls Uebersicht ausgeschaltet
		if (!speicherUebersichtAktiviert) return;

		bool[] genutzt = speicher.getGenutzt();

		//Zwecks tabellarischer Form, werden zwei Textobjekte benutzt
		String linkerString = "", rechterString = "";

		int anzahlZeilen = 0;

		for (int i = 0; i < speicherLaenge; i++)
		{
			if (genutzt[i])
			{
				anzahlZeilen++;

				linkerString += ("[" + i + "]:\n");

				switch (darstellung)
				{
					case Darstellung.Binaer:
						rechterString += speicher[i].getAsBinaerString();
						break;

					case Darstellung.Dezimal:
						rechterString += (int) speicher[i];
						break;

					case Darstellung.Befehl:
						rechterString += speicher[i].getAsBefehlString(true);
						break;
				}

				rechterString += "\n";
			}
		}

		//Laenge des Textfeldes anpassen, 34 entspricht in etwa der Hohe einer Zeile
		textWrapperSpeicherUebersicht.sizeDelta = new Vector2(370, 34*anzahlZeilen);

		linkeSpeicherUebersicht.text = linkerString;
		rechteSpeicherUebersicht.text = rechterString;
	}


	void letzteBefehleUpdate ()
	{
		if (letzteBefehle.Count >= 11)
			letzteBefehle.Dequeue();

		string neuerString = "";
		foreach (Wort befehl in letzteBefehle)
		{
			neuerString = befehl.getAsBefehlString(false) + "\n" + neuerString;
		}

		letzteBefehleText.text = neuerString;
	}


	public void befehlsButtonCallA (bool a)
	{
		if (maschineEingeschaltet)
		{
			pult[14] = a;
			knoepfe[14].setUnteresLicht(a);
		}
	}


	public void befehlsButtonCallB (bool b)
	{
		if (maschineEingeschaltet)
		{
			pult[13] = b;
			knoepfe[13].setUnteresLicht(b);
		}
	}


	public void befehlsButtonCallC (bool c)
	{
		if (maschineEingeschaltet)
		{
			pult[12] = c;
			knoepfe[12].setUnteresLicht(c);
		}
	}


	public void befehlsButtonCallD (bool d)
	{
		if (maschineEingeschaltet)
		{
			pult[11] = d;
			knoepfe[11].setUnteresLicht(d);
		}
	}


	public void befehlsButtonCallE (bool e)
	{
		if (maschineEingeschaltet)
		{
			pult[10] = e;
			knoepfe[10].setUnteresLicht(e);
		}
	}


	public Speicher speicherGet ()
	{
		return speicher;
	}


	public void speicherSet (Speicher neuerSpeicher)
	{
		speicher = neuerSpeicher;
		speicheruebsichtUpdate();
	}


	public bool getEinAus ()
	{
		return maschineEingeschaltet;
	}
}
