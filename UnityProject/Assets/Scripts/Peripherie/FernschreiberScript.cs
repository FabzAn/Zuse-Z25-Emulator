using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class FernschreiberScript : PeripherieScript {

    public FernschreiberTastenScript[] tasten;
    public TypenhebelScript typenhebel;
    public GameObject lampe;
    public LochstreifenstanzerScript stanzer;

    public KameraZielScript kameraZiel;

    public bool lochstreifenDrucken = false;

    //Zur Unterscheidung, ob zu Text oder Befehlswoertern kodiert werden soll
    bool eingabeIstText = false;
    //Da zwei einzelne ' eingegeben werden muessen, wird hiermit das erste ' gemerkt
    bool einApostroph = false;
    string befehlsBuffer = "";

    //Das hier ist fuer die Ausgabe
    bool buchstabenZeichen = true;

    //Falls ein Umspeicher Bandbefehl gegeben wurde, wir die Zieladresse zwischengespeichert
    int bandBefehlZieladresse = 0;
    bool bandBefehlStehtNochAus = false;

    List<Wort> outputBuffer = new List<Wort>();

    //modus true bedeutet Fernschreiber ist aktiv
    public enum Modus
    {
        Warten,
        Bringtransfer,
        Schaltimpuls
    }
    Modus _modus;
    Modus modus
    {
        get
        {
            return _modus;
        }
        set
        {
            _modus = value;

            if (_modus != Modus.Warten)
            {
                //Lampe ein
                lampe.SetActive(true);
                kameraZiel.kameraHierher();
            }
            else
            {
                //Lampe aus
                lampe.SetActive(false);

                zeichenZaehler = 0;
                einApostroph = false;
                bandBefehlStehtNochAus = false;

                //Zu viel eingegebene Zeichen werden verworfen
                befehlsBuffer = "";
                eingabeIstText = false;
                outputBuffer.Clear();
            }
        }
    }


    int zeichenZaehler = 0;


    public float knopfDruckVerzoegerung;

    //True wenn das Alphabet eingegeben werden kann, false wenn Zahlen eingegeben werden koennen.
    bool alphabetEingabe = true;
    //....., Progr., Bed. Schalter, Lampe, Dauer, Schritt
    bool[] kippschalter = {false, false, false, false, false ,false};


    void Update ()
    {
        tastaturInput();
    }


    public override void reset ()
    {
        //Mit Moduswechsel zu Warten werden immer auch die uebrigen Variablen zurueckgesetzt
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
            modus = Modus.Bringtransfer;
            if (outputBuffer.Count == 0)
    		      StartCoroutine(bringenVervollstaendigen());
            else
            {
                rechenwerk.bringenCallBack(outputBuffer[0]);
                outputBuffer.RemoveAt(0);
            }
        }
	}


    IEnumerator bringenVervollstaendigen ()
    {
        while (befehlsBuffer == "")
        {
            yield return null;
        }

        Wort ausgabe = kodieren(befehlsBuffer.Substring(0,1));
        befehlsBuffer = befehlsBuffer.Substring(1);
        rechenwerk.bringenCallBack(ausgabe);

        modus = Modus.Warten;
    }


	public override void umspeichern(Wort wort)
	{
        if (peripherie.freigabe)
		      typenhebel.einreihen(dekodieren(wort.getRange(13, 17) >> 13, buchstabenZeichen, out buchstabenZeichen));
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


    public override void umspeichertransfer(Wort[] inhalt)
    {
        if (peripherie.freigabe)
        {
            string ausgabe = textRueckParser(inhalt);
            foreach (char c in ausgabe)
                typenhebel.einreihen(c);
        }
    }


    /*  Fuer Sonderzeichen in Klammern die Kodierung
        Kodierung entspricht groessenteils CCITT-2-Code

        Folgende Tasten gehoeren zum Ziffernbereicht
        *
        ]
        -
        '
        8
        7
        Wer da? (!)
        4
        ; (oder Klingel)
        ,
        [
        :
        (
        5
        +
        )
        2
        tiefe 10 (_)
        6
        0
        1
        9
        Andreaskreuz (äquivalent zu ?)
        ]
        .
        /
        =


    Folgende Tasten gehoeren zum Buchstabenbereich
        #
        E
        A
        S
        I
        U
        D
        R
        J
        N
        F
        C
        K
        T
        Z
        L
        W
        H
        Y
        P
        Q
        O
        B
        G
        M
        X
        V


    Folgende Tasten gehoeren zu beiden Bereichen
        Zeilentransport (§)
        Zwischenraum (Ich gehe davon aus, dass das Leerzeichen gemeint ist, auch wenn an der original Maschine zwei Leerzeichen als Trennzeichen benutzt werden)
        Wagenruecklauf (<)
        Ziffernumschaltzeichen ($)
        Buchstabenumschaltzeichen (&)

        Trennzeichen sind: wagenruecklauf, zeilentransport (naechste Zeile) und Zwischenraum (Als Zwischenraum interpretiere ich zwei Leerzeichen. So funktioniert es nämlich bei der Maschine im Arithmeum)

    */


    //Dient dazu, dass nicht zwischen Alphabet und Nummer gewechselt wird, ohne A... oder 1... zu druecken
    public void tastenDruck (int tastenNr)
    {
        //Die gedrueckte Taste ist nicht Wagenruecklauf, Zeilentransport, 1..., leerzeichen oder A...
        if (!new []{28, 43, 56, 57, 58}.Contains(tastenNr))
        {
            //Gedrueckte Taste ist im Zahlenbereich
            if (new []{0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 24, 25,
                        26, 27, 39, 40, 41, 42, 51, 52, 53, 54, 55}.Contains(tastenNr))
            {
                if (alphabetEingabe)
                {
                    //Taste 1... muss zunaechst gedrueckt werden
                    tasten[56].druecken();
                    StartCoroutine(verzoegertesDruecken(tastenNr));
                    return;
                }
            }
            //Gedrueckte Taste ist im Buchstabenbereich
            else
            {
                if (!alphabetEingabe)
                {
                    //Taste A... muss zunaechst gedrueckt werden
                    tasten[58].druecken();
                    StartCoroutine(verzoegertesDruecken(tastenNr));
                    return;
                }
            }
        }

        //Gedrueckte Taste ist Wagenruecklauf, Zeilentransport, 1..., leerzeichen oder A...
        tasten[tastenNr].druecken();
    }

    //drucken gibt an, ob auf den Fernschreiber ausgegeben werden soll
    void schreiben (string inhalt, bool drucken = true)
    {
        if (drucken && lochstreifenDrucken)
        {
            stanzer.stanzen(kodieren(inhalt));
        }

        //befehlsBuffer wird gefuellt, bis ein Befehl vollstaendig ist oder ein Textblock zuende
        if (eingabeIstText)
        {
            //Textblock zuende
            if (inhalt == "'" && einApostroph)
            {
                //Es ist wichtig das erst geparst wird, damit der Parser weiss, dass es sich um Text handelt
                befehlNachOutputBuffer(textParser(befehlsBuffer));
                eingabeIstText = false;
            }
            //Erster Apostroph
            else if (inhalt == "'")
                einApostroph = true;
            else
            {
                if (einApostroph)
                {
                    einApostroph = false;
                    //Da das vorige Apostroph nicht zum Ende der Texteingabe gehoerte, muss es nachgereicht werden
                    befehlsBuffer += "'";
                    zeichenZaehler++;
                    if (zeichenZaehler == 3)
                    {
                        befehlNachOutputBuffer(textParser(befehlsBuffer));
                        zeichenZaehler = 0;
                    }
                }
                befehlsBuffer += inhalt;
                zeichenZaehler++;

                //Jeweils drei Zeichen werden zu einem Wort codiert
                if (zeichenZaehler == 3)
                {
                    befehlNachOutputBuffer(textParser(befehlsBuffer));
                    zeichenZaehler = 0;
                }
            }
        }
        else
        {
            //Zwei ' hintereinander bedeuten, dass ein Texblock startet
            if (inhalt == "'" && einApostroph)
            {
                //Der unfertige vorige Befehl wird verworfen
                befehlsBuffer = "";
                eingabeIstText = true;
                einApostroph = false;
            }
            else if (inhalt == "'")
            {
                einApostroph = true;
            }
            else
            {
                //Falls ein Trennzeichen eingegeben wurde wird versucht den Befehl zu parsen
                if (new []{" ", "<", "§"}.Contains(inhalt))
                {
                    befehlNachOutputBuffer(befehlsParser(befehlsBuffer));
                }
                else
                {
                    befehlsBuffer += inhalt;
                    if (einApostroph)
                        einApostroph = false;
                }
            }
        }


        //Falls gewuenscht wird das Schriftzeichen auf den Fernschreiber gedruckt
        if (drucken)
            typenhebel.einreihen(inhalt);
    }


    //Ein paar Formatvorlagen, die ich im befehlsParser() brauche
    //Befehle mit einem Buchstaben
    static readonly Regex einfach = new Regex(@"^[A-Z][\+\-]?\d+$");
    //Befehle mit zwei Buchstaben
    static readonly Regex zweifach = new Regex(@"^[A-Z]{2}[\+\-]?\d+$");
    //Befehle mit drei Buchstaben
    static readonly Regex dreifach = new Regex(@"^[A-Z]{3}[\+\-]?\d+$");
    //Befehle mit vier Buchstaben
    static readonly Regex vierfach = new Regex(@"^[A-Z]{4}[\+\-]?\d+$");
    //SHRVW, MB, X und Y haben eigene Regular Expressions
    static readonly Regex SHRVWRegex = new Regex(@"^SHRVW\d+$");
    static readonly Regex mbRegex = new Regex(@"^MB\(?(\d+\+)?\d+\)?$");
    static readonly Regex xyRegex = new Regex(@"^[XY]\(?(\d+\+)?\d+\)?$");
    static readonly Regex bandBefehlRegex = new Regex(@"^[EU]\d*\+?\d+[EU]$");
    //Zahlen koennen ohne Komma, mit Komma oder als halblogarithmische Darstellung gegeben werden
    static readonly Regex zahlRegex = new Regex(@"^[\+\-]?\d+$");
    static readonly Regex kommaZahlRegex = new Regex(@"^[\+\-]?\d*\.\d*$");
    static readonly Regex halblogaRegex = new Regex(@"^[\+\-]?\d*\.?\d*\_[\+\-]?\d+$");



    //An dieser Stelle muessen 40 verschiedene Befehle plus Parameter voneinander unterschieden werden
    //Der zweite Wert wird gesetzt, falls eine Zahl in zwei Speicherzellen kodiert
    //Wenn einer der beiden Werte nicht beachtet werden soll, ist er -1
    public int[] befehlsParser (string parseThis)
    {
        int[] ausgabe = {0, -1};
        //Faelle $& und && zeigen Irrungen an. Alle bis dahin eingegebenen Zeichen weren geloescht
        while (parseThis.Contains("$&"))
            parseThis = parseThis.Substring(1);
        while (parseThis.Contains("&&"))
            parseThis = parseThis.Substring(1);

        //Falls der Befehl von Leerzeichen * oder # angefuehrt wird, werden diese geloescht
        while (parseThis != "" && (parseThis[0] == '*' || parseThis[0] == '#'))
            parseThis = parseThis.Substring(1);

        //Alle weiteren unsichtbaren Zeichen werden einfach so entfernt.
        parseThis = parseThis.Replace("$", "");
        parseThis = parseThis.Replace("&", "");

        //Die hier aufgefuehrten Zeichen sind Shortcuts fuer die entsprechenden Befehle
        if (new []{":", "=", "(", ")", "[", "]", "/", "?", "!"}.Contains(parseThis))
        {
            switch (parseThis)
            {
                case ":":
                    parseThis = "E101E";
                    break;
                case "=":
                    parseThis = "E102E";
                    break;
                case "(":
                    parseThis = "E103E";
                    break;
                case ")":
                    parseThis = "E104E";
                    break;
                case "[":
                    parseThis = "E105E";
                    break;
                case "]":
                    parseThis = "E106E";
                    break;
                case "/":
                    parseThis = "E107E";
                    break;
                case "?":
                    parseThis = "E108E";
                    break;
                case "!":
                    parseThis = "E109E";
                    break;
            }
        }

        //String vorbereitung zuende

        //Es ist moeglich, dass parseThis leer ist. Dann wirft .Substring einen Error
        if (parseThis == "")
            return new []{-4, -1};

        //Falls ein Bedingungsbuchstabe geschrieben wurde, kann der Befehl keine Zahl mehr sein
        bool kannZahlSein = true;

        //Bedingungsbits werden ueberprueft
        if (parseThis.Substring(0,1) == "P")
        {
            ausgabe[0] = IntFunktionen.setStelle(ausgabe[0], 17, true);
            parseThis = parseThis.Substring(1);

            //Es ist moeglich, dass parseThis leer ist. Dann wirft .Substring einen Error
            if (parseThis == "")
                return new []{-1, -1};

            kannZahlSein = false;
        }

        if (parseThis.Substring(0,1) == "Q")
        {
            ausgabe[0] = IntFunktionen.setStelle(ausgabe[0], 16, true);
            parseThis = parseThis.Substring(1);

            //Es ist moeglich, dass parseThis leer ist. Dann wirft .Substring einen Error
            if (parseThis == "")
                return new []{-1, -1};

            kannZahlSein = false;
        }

        if (parseThis.Substring(0,1) == "G")
        {
            ausgabe[0] = IntFunktionen.setStelle(ausgabe[0], 15, true);
            parseThis = parseThis.Substring(1);

            kannZahlSein = false;
        }


        //MB und X/Y sind vorgezogen, da sie sich mit einfach bzw. zweifach ueberschneiden koennen
        if (mbRegex.IsMatch(parseThis))
        {
            int befehlsTeil = 25;
            ausgabe[0] |= (befehlsTeil << 10);

            parseThis = parseThis.Replace("(", "");
            parseThis = parseThis.Replace(")", "");

            int stellePlus = parseThis.IndexOf("+");

            //Falls kein Plus gefunden wurde, ist der erste Teil implizit 0
            if (stellePlus == -1)
            {
                parseThis = parseThis.Insert(2, "0+");
                stellePlus = parseThis.IndexOf("+");
            }

            //a und b werden aus dem String extrahiert
            int ersteZahl, zweiteZahl;

            if (!int.TryParse(parseThis.Substring(2, stellePlus - 2), out ersteZahl))
            {
                return new []{-1, -1};
            }

            if (!int.TryParse(parseThis.Substring(stellePlus + 1), out zweiteZahl))
            {
                return new []{-1, -1};
            }

            //ersteZahl muss auf Stellen 7 bis 9 geshifted werden
            ersteZahl <<= 7;
            ersteZahl |= IntFunktionen.getRange(zweiteZahl, 0, 2);
            ausgabe[0] |= IntFunktionen.getRange(ersteZahl, 0, 9);


            return ausgabe;
        }
        else if (xyRegex.IsMatch(parseThis))
        {
            int befehlsTeil = 26;
            ausgabe[0] |= (befehlsTeil << 10);

            parseThis = parseThis.Replace("(", "");
            parseThis = parseThis.Replace(")", "");

            if (parseThis[0] == 'Y')
                ausgabe[0] = IntFunktionen.setStelle(ausgabe[0], 9, true);
            else if (parseThis[0] != 'X')
            {
                return new []{-1, -1};
            }


            int stellePlus = parseThis.IndexOf("+");

            //Falls kein Plus gefunden wurde, wird das aequivalente "0+" eingefuegt
            if (stellePlus == -1)
            {
                parseThis = parseThis.Insert(1, "0+");
                stellePlus = parseThis.IndexOf("+");
            }

            //g und s werden aus dem String extrahiert und kommen in Stellen 5-6 bzw. 0-4
            int ersteZahl, zweiteZahl;

            if (!int.TryParse(parseThis.Substring(1, stellePlus - 1), out ersteZahl))
            {
                return new []{-1, -1};
            }

            if (!int.TryParse(parseThis.Substring(stellePlus + 1), out zweiteZahl))
            {
                return new []{-1, -1};
            }

            ersteZahl <<= 5;
            ersteZahl |= IntFunktionen.getRange(zweiteZahl, 0, 4);

            ausgabe[0] |= IntFunktionen.getRange(ersteZahl, 0, 6);

            return ausgabe;
        }
        //Befehle mit einem Buchstaben gefolgt von einer Zahl
        else if (einfach.IsMatch(parseThis))
        {
            int befehlsTeil = 0;
            switch (parseThis[0])
            {
                case 'A':
                    befehlsTeil = 1;
                    break;
                case 'B':
                    befehlsTeil = 3;
                    break;
                case 'D':
                    befehlsTeil = 19;
                    break;
                case 'E':
                    befehlsTeil = 31;
                    break;
                case 'F':
                    befehlsTeil = 30;
                    break;
                case 'H':
                    befehlsTeil = 24;
                    break;
                case 'I':
                    befehlsTeil = 5;
                    break;
                case 'K':
                    befehlsTeil = 0;
                    break;
                case 'M':
                    befehlsTeil = 18;
                    break;
                case 'S':
                    befehlsTeil = 14;
                    break;
                case 'T':
                    befehlsTeil = 29;
                    break;
                case 'U':
                    befehlsTeil = 16;
                    break;
                case 'Z':
                    befehlsTeil = 27;
                    break;
                default:
                    return new []{-1, -1};
            }

            ausgabe[0] |= (befehlsTeil << 10);



            int parameterTeil;
            if (!int.TryParse(parseThis.Substring(1), out parameterTeil))
            {
                return new []{-1, -1};
            }
            //Falls die gegebene Zahl zu groß ist wird sie auf 10 bit gestutzt
            ausgabe[0] |= IntFunktionen.getRange(parameterTeil, 0, 9);

            return ausgabe;
        }
        else if (zweifach.IsMatch(parseThis))
        {
            int befehlsTeil = 0;
            switch (parseThis.Substring(0, 2))
            {
                case "AA":
                    befehlsTeil = 2;
                    break;
                case "BB":
                    befehlsTeil = 4;
                    break;
                case "BT":
                    befehlsTeil = 23;
                    break;
                case "CA":
                    befehlsTeil = 7;
                    break;
                case "CB":
                    befehlsTeil = 8;
                    break;
                case "CI":
                    befehlsTeil = 9;
                    break;
                case "CS":
                    befehlsTeil = 10;
                    break;
                case "CT":
                    befehlsTeil = 11;
                    break;
                case "DI":
                    befehlsTeil = 6;
                    break;
                case "IS":
                    befehlsTeil = 12;
                    break;
                case "NS":
                    befehlsTeil = 13;
                    break;
                case "SS":
                    befehlsTeil = 15;
                    break;
                case "ST":
                    befehlsTeil = 28;
                    break;
                case "TI":
                    befehlsTeil = 21;
                    break;
                case "UT":
                    befehlsTeil = 22;
                    break;
                case "UU":
                    befehlsTeil = 17;
                    break;
                default:
                    return new []{-1, -1};
            }

            ausgabe[0] |= (befehlsTeil << 10);


            int parameterTeil;
            if (!int.TryParse(parseThis.Substring(2), out parameterTeil))
            {
                return new []{-1, -1};
            }

            //Falls die gegebene Zahl zu groß ist wird sie auf 10 bit gestutzt
            ausgabe[0] |= IntFunktionen.getRange(parameterTeil, 0, 9);

            return ausgabe;
        }
        else if (dreifach.IsMatch(parseThis))
        {
            //Es kommen nur SHL und SHR in Frage
            int befehlsTeil = 20;
            ausgabe[0] |= (befehlsTeil << 10);

            switch (parseThis.Substring(0, 3))
            {
                case "SHL":
                    ausgabe[0] = IntFunktionen.setStelle(ausgabe[0], 9, true);
                    break;
                case "SHR":
                    ausgabe[0] = IntFunktionen.setStelle(ausgabe[0], 8, true);
                    break;
                default:
                    return new []{-1, -1};
            }


            int parameterTeil;
            if (!int.TryParse(parseThis.Substring(3), out parameterTeil))
            {
                return new []{-1, -1};
            }

            //Falls die gegebene Zahl zu groß ist wird sie auf 6 bit gestutzt
            ausgabe[0] |= IntFunktionen.getRange(parameterTeil, 0, 5);

            return ausgabe;
        }
        else if (vierfach.IsMatch(parseThis))
        {
            //Es kommen nur SHLV, SHRV und SHRW in Frage
            int befehlsTeil = 20;
            ausgabe[0] |= (befehlsTeil << 10);

            switch (parseThis.Substring(0, 4))
            {
                case "SHLV":
                    ausgabe[0] = IntFunktionen.setStelle(ausgabe[0], 9, true);
                    ausgabe[0] = IntFunktionen.setStelle(ausgabe[0], 7, true);
                    break;
                case "SHRV":
                    ausgabe[0] = IntFunktionen.setStelle(ausgabe[0], 8, true);
                    ausgabe[0] = IntFunktionen.setStelle(ausgabe[0], 7, true);
                    break;
                case "SHRW":
                    ausgabe[0] = IntFunktionen.setStelle(ausgabe[0], 8, true);
                    ausgabe[0] = IntFunktionen.setStelle(ausgabe[0], 6, true);
                    break;
                default:
                    return new []{-1, -1};
            }


            int parameterTeil;
            if (!int.TryParse(parseThis.Substring(4), out parameterTeil))
            {
                return new []{-1, -1};
            }

            //Falls die gegebene Zahl zu groß ist wird sie auf 6 bit gestutzt
            ausgabe[0] |= IntFunktionen.getRange(parameterTeil, 0, 5);

            return ausgabe;
        }
        else if (SHRVWRegex.IsMatch(parseThis))
        {
            int befehlsTeil = 20;
            ausgabe[0] |= (befehlsTeil << 10);

            ausgabe[0] = IntFunktionen.setStelle(ausgabe[0], 8, true);
            ausgabe[0] = IntFunktionen.setStelle(ausgabe[0], 7, true);
            ausgabe[0] = IntFunktionen.setStelle(ausgabe[0], 6, true);

            int parameterTeil;
            if (!int.TryParse(parseThis.Substring(5), out parameterTeil))
            {
                return new []{-1, -1};
            }

            //Falls die gegebene Zahl zu groß ist wird sie auf 6 bit gestutzt
            ausgabe[0] |= IntFunktionen.getRange(parameterTeil, 0, 5);

            return ausgabe;
        }
        else if (bandBefehlRegex.IsMatch(parseThis))
        {
            //Bandbefehl gibt {-2, zieladresse} zurueck im Falle UmU
            //{-3, -zieladresse}, falls EmE

            //Die RegularExpression schließt Befehle UmE oder EmU noch nicht aus
            if (parseThis[0] != parseThis[parseThis.Length - 1])
            {
                return new []{-1, -1};
            }

            int bereichsangabe = 0;
            int zieladresse = 0;

            //Pruefe ob und wo sich ein Plus im Befehl befindet
            int stellePlus = parseThis.IndexOf("+");

            //Falls kein Plus gefunden wurde, ist die Bereichsangabe implizit 0
            if (stellePlus != -1)
            {
                if (!int.TryParse(parseThis.Substring(1, stellePlus - 1), out bereichsangabe))
                {
                    return new []{-1, -1};
                }
            }
            else
                stellePlus = 0;

            //Die Bereichsangabe kommt in 10-14
            bereichsangabe = (int) IntFunktionen.getRange(bereichsangabe, 0, 4) << 10;

            //buffer.Length - 2 weil am Ende noch ein Buchstabe steht
            if (!int.TryParse(parseThis.Substring(stellePlus + 1, parseThis.Length - 2 - stellePlus), out zieladresse))
            {
                return new []{-1, -1};
            }

            zieladresse += bereichsangabe;
            ausgabe[1] = zieladresse;


            //Umspeicher Bandbefehl
            if (parseThis[0] == 'U')
            {
                ausgabe[0] = -2;
            }

            //Sprung Bandbefehl
            else
            {
                ausgabe[0] = -3;
            }

            return ausgabe;
        }
        else if (zahlRegex.IsMatch(parseThis) && kannZahlSein)
        {
            long zahl;

            if (!long.TryParse(parseThis, out zahl))
            {
                return new []{-1, -1};
            }

            //Falls mehr als 5 Dezimalstellen benutzt werden, wird die Zahl als Doppelwort gespeichert
            //Dann wird erst das kleiner Wort uebergeben
            if(parseThis.Length > 5)
            {
                Doppelwort ausgabeWert = zahl;
                ausgabe[0] = (int) ausgabeWert.getUnteresWort();
                ausgabe[1] = (int) ausgabeWert.getOberesWort();
                return ausgabe;
            }
            else
            {
                ausgabe[0] = (int) zahl;
                return ausgabe;
            }
        }
        else if (kommaZahlRegex.IsMatch(parseThis) && kannZahlSein)
        {
            //Pruefe wo sich das Komma in der Zahl befindet
            int kommaStelle = parseThis.IndexOf(".");

            //Es wird nun ermittelt wie weit das Komma verschoben werden muesste um an letzter Stelle zu stehen
            kommaStelle = parseThis.Length - 1 - kommaStelle;
            //Diese Zahl muss negiert werden um den Exponenten der halblogarithmischen Darstellung zu bekommen
            kommaStelle = -kommaStelle;
            //Der Exponent wird in die zweite Speicherstelle. Zu beachten: Exponent zu Basis 10 nicht zu Basis 2

            parseThis = parseThis.Replace(".", "");

            int zahl;

            if (!int.TryParse(parseThis, out zahl))
            {
                return new []{-1, -1};
            }

            ausgabe[0] = zahl;
            ausgabe[1] = kommaStelle;
            return ausgabe;
        }
        else if (halblogaRegex.IsMatch(parseThis) && kannZahlSein)
        {
            //Pruefe wo sich das Basis Symbol (die tiefgestellte 10) in der Zahl befindet
            int tiefeZehnStelle = parseThis.IndexOf("_");

            //Pruefe wo sich das Komma in der Zahl befindet
            //Falls die Zahl ohne Komma eingegeben wurde ist das Komma implizit an letzter Stelle vor _
            int kommaStelle = parseThis.IndexOf(".");
            bool kommaVorhanden = (kommaStelle != -1);
            if (!kommaVorhanden)
                kommaStelle = tiefeZehnStelle - 1;

            //Es wird nun ermittelt wie weit das Komma verschoben werden muesste um an letzter Stelle vor _ zu stehen
            kommaStelle = tiefeZehnStelle - 1 - kommaStelle;
            //Diese Zahl muss negiert werden um den Exponenten der halblogarithmischen Darstellung zu bekommen
            kommaStelle = -kommaStelle;
            //Der Exponent wird in die zweite Speicherstelle. Zu beachten: Exponent zu Basis 10 nicht zu Basis 2

            //Das Komma muss noch entfernt werden
            parseThis = parseThis.Replace(".", "");

            //Falls tatsaechlich ein Komma entfernt wurde, muss die verlorengegangene stelle rausgerechnet werden
            if (kommaVorhanden)
            {
                tiefeZehnStelle--;
            }

            //Der angegebene Exponent muss gefunden und mit der Kommaverschiebung verrechnet werden
            int exponent;
            //Exponent wird geparst
            if (!int.TryParse(parseThis.Substring(tiefeZehnStelle + 1), out exponent))
            {
                return new []{-1, -1};
            }

            kommaStelle += exponent;

            //Erst hier wird der eigentlich Teil der Zahl behandelt
            int zahl;

            if (!int.TryParse(parseThis.Substring(0, tiefeZehnStelle), out zahl))
            {
                return new []{-1, -1};
            }

            ausgabe[0] = zahl;
            ausgabe[1] = kommaStelle;
            return ausgabe;
        }
        else
        {
            return new []{-1, -1};
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
                //Fehlermedlung wird auf den Fernschreiber ausgegeben
                typenhebel.einreihen(" BDBF31+446 ");
				return;
			//U Bandbefehl
            case -2:
                bandBefehlZieladresse = inhalt[1];
                bandBefehlStehtNochAus = true;
                return;
			case -3:
				rechenwerk.bandBefehlSprung(inhalt[1]);
				modus = Modus.Warten;
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


    public int[] textParser (string parseThis)
    {
        int aktuellesZeichen;
        int ausgabeWort = 0;

        for (int zeichenNr = 0; zeichenNr < 3; zeichenNr++)
        {
            //Das Zeichen wird sofort decodiert und an die richtige Stelle geshifted
            if (parseThis != "")
            {
                aktuellesZeichen = kodieren(parseThis.Substring(0,1));
                parseThis = parseThis.Substring(1);
            }
            //Es wird mit Leerzeichen aufgefuellt (* und # werden in "Ablochvorschrift" im Handbuch als Leerzeichen bezeichnet)
            else
                aktuellesZeichen = kodieren("*");

            //Dieser Shift sorgt dafuer, dass das letzte Zeichen immer in 0-4 steht. Zwischen den Zeichen sind Nullen
            ausgabeWort <<= 6;

            //Aktuelles Zeichen wird in das zu bearbeitende Wort eingefuegt
            ausgabeWort |= aktuellesZeichen;
        }

        //Zum Schluss wird noch einmal geshifted damit das erste Zeichen in 13-17 steht
        ausgabeWort <<= 1;

        return new[]{ausgabeWort, -1};
    }


    public int kodieren (string inhalt)
    {
        switch (inhalt)
        {
            case "*":
            case "#":
                return 0;
            case "3":
            case "E":
                return 1;
            case "§":
                return 2;
            case "-":
            case "A":
                return 3;
            case " ":
                return 4;
            case "'":
            case "S":
                return 5;
            case "8":
            case "I":
                return 6;
            case "7":
            case "U":
                return 7;
            case "<":
                return 8;
            case "!":
            case "D":
                return 9;
            case "4":
            case "R":
                return 10;
            case ";":
            case "J":
                return 11;
            case ",":
            case "N":
                return 12;
            case "[":
            case "F":
                return 13;
            case ":":
            case "C":
                return 14;
            case "(":
            case "K":
                return 15;
            case "5":
            case "T":
                return 16;
            case "+":
            case "Z":
                return 17;
            case ")":
            case "L":
                return 18;
            case "2":
            case "W":
                return 19;
            case "_":
            case "H":
                return 20;
            case "6":
            case "Y":
                return 21;
            case "0":
            case "P":
                return 22;
            case "1":
            case "Q":
                return 23;
            case "9":
            case "O":
                return 24;
            case "?":
            case "B":
                return 25;
            case "]":
            case "G":
                return 26;
            case "$":
                return 27;
            case ".":
            case "M":
                return 28;
            case "/":
            case "X":
                return 29;
            case "=":
            case "V":
                return 30;
            case "&":
                return 31;
        }
        //Leerzeichen wird ausgegeben falls das Zeichen unbekannt ist
        return 4;
    }


    string textRueckParser (Wort[] inhalt)
    {
        string ausgabe = "";

        int verschiebung = 0;
        long aktuellerWert;

        foreach (Wort aktuellesWort in inhalt)
        {
            for (int i = 0; i < 3; i++)
            {
                verschiebung = i*6;
                //Erster Wert steht in 13-17, Zweiter in 7-11, Dritter in 1-5, dazwischen Nullen
                aktuellerWert = aktuellesWort.getRange(13 - verschiebung, 17 - verschiebung) >> 13 - verschiebung;

                ausgabe += dekodieren(aktuellerWert, buchstabenZeichen, out buchstabenZeichen);
            }
        }

        return ausgabe;
    }


    public char dekodieren (int nr, bool buchstabenOderNicht, out bool buchstabenOderNichtNachher)
    {
        return dekodieren ((long) nr, buchstabenOderNicht, out buchstabenOderNichtNachher);
    }


    char dekodieren (long nr, bool buchstabenOderNicht, out bool buchstabenOderNichtNachher)
    {
        buchstabenOderNichtNachher = buchstabenOderNicht;

        switch(nr)
        {
            case 0:
                return (buchstabenOderNicht ? '#' : '*');
            case 1:
                return (buchstabenOderNicht ? 'E' : '3');
            case 2:
                return '§';
            case 3:
                return (buchstabenOderNicht ? 'A' : '-');
            case 4:
                return ' ';
            case 5:
                return (buchstabenOderNicht ? 'S' : '\'');
            case 6:
                return (buchstabenOderNicht ? 'I' : '8');
            case 7:
                return (buchstabenOderNicht ? 'U' : '7');
            case 8:
                return '<';
            case 9:
                return (buchstabenOderNicht ? 'D' : '!');
            case 10:
                return (buchstabenOderNicht ? 'R' : '4');
            case 11:
                return (buchstabenOderNicht ? 'J' : ';');
            case 12:
                return (buchstabenOderNicht ? 'N' : ',');
            case 13:
                return (buchstabenOderNicht ? 'F' : '[');
            case 14:
                return (buchstabenOderNicht ? 'C' : ':');
            case 15:
                return (buchstabenOderNicht ? 'K' : '(');
            case 16:
                return (buchstabenOderNicht ? 'T' : '5');
            case 17:
                return (buchstabenOderNicht ? 'Z' : '+');
            case 18:
                return (buchstabenOderNicht ? 'L' : ')');
            case 19:
                return (buchstabenOderNicht ? 'W' : '2');
            case 20:
                return (buchstabenOderNicht ? 'H' : '_');
            case 21:
                return (buchstabenOderNicht ? 'Y' : '6');
            case 22:
                return (buchstabenOderNicht ? 'P' : '0');
            case 23:
                return (buchstabenOderNicht ? 'Q' : '1');
            case 24:
                return (buchstabenOderNicht ? 'O' : '9');
            case 25:
                return (buchstabenOderNicht ? 'B' : '?');
            case 26:
                return (buchstabenOderNicht ? 'G' : ']');
            case 27:
                buchstabenOderNichtNachher = false;
                return '$';
            case 28:
                return (buchstabenOderNicht ? 'M' : '.');
            case 29:
                return (buchstabenOderNicht ? 'X' : '/');
            case 30:
                return (buchstabenOderNicht ? 'V' : '=');
            case 31:
                buchstabenOderNichtNachher = true;
                return '&';
            default:
                return ' ';
        }
    }


    public void kippschalterUmlegen (int schalterNr, bool neuerWert)
    {
        kippschalter[schalterNr] = neuerWert;
    }


    IEnumerator verzoegertesDruecken(int tastenNr)
    {
        yield return new WaitForSeconds(knopfDruckVerzoegerung);
        tasten[tastenNr].druecken();
    }


    void tastaturInput()
    {
        switch (Input.inputString)
        {
            case "[":
                tastenDruck(0);
                break;
            case "]":
                tastenDruck(1);
                break;
            case ";":
                tastenDruck(2);
                break;
            case "?":
                tastenDruck(3);
                break;
            case "/":
                tastenDruck(4);
                break;
            case "'":
                tastenDruck(5);
                break;
            case "=":
                tastenDruck(6);
                break;
            case "(":
                tastenDruck(7);
                break;
            case ")":
                tastenDruck(8);
                break;
            case "7":
                tastenDruck(10);
                break;
            case "8":
                tastenDruck(11);
                break;
            case "9":
                tastenDruck(12);
                break;
            case "-":
                tastenDruck(13);
                break;
            case "q":
            case "Q":
                tastenDruck(14);
                break;
            case "w":
            case "W":
                tastenDruck(15);
                break;
            case "e":
            case "E":
                tastenDruck(16);
                break;
            case "r":
            case "R":
                tastenDruck(17);
                break;
            case "t":
            case "T":
                tastenDruck(18);
                break;
            case "z":
            case "Z":
                tastenDruck(19);
                break;
            case "u":
            case "U":
                tastenDruck(20);
                break;
            case "i":
            case "I":
                tastenDruck(21);
                break;
            case "o":
            case "O":
                tastenDruck(22);
                break;
            case "p":
            case "P":
                tastenDruck(23);
                break;
            case "4":
                tastenDruck(24);
                break;
            case "5":
                tastenDruck(25);
                break;
            case "6":
                tastenDruck(26);
                break;
            case "a":
            case "A":
                tastenDruck(29);
                break;
            case "s":
            case "S":
                tastenDruck(30);
                break;
            case "d":
            case "D":
                tastenDruck(31);
                break;
            case "f":
            case "F":
                tastenDruck(32);
                break;
            case "g":
            case "G":
                tastenDruck(33);
                break;
            case "h":
            case "H":
                tastenDruck(34);
                break;
            case "j":
            case "J":
                tastenDruck(35);
                break;
            case "k":
            case "K":
                tastenDruck(36);
                break;
            case "l":
            case "L":
                tastenDruck(37);
                break;
            case "#":
                tastenDruck(38);
                break;
            case "1":
                tastenDruck(39);
                break;
            case "2":
                tastenDruck(40);
                break;
            case "3":
                tastenDruck(41);
                break;
            case "y":
            case "Y":
                tastenDruck(44);
                break;
            case "x":
            case "X":
                tastenDruck(45);
                break;
            case "c":
            case "C":
                tastenDruck(46);
                break;
            case "v":
            case "V":
                tastenDruck(47);
                break;
            case "b":
            case "B":
                tastenDruck(48);
                break;
            case "n":
            case "N":
                tastenDruck(49);
                break;
            case "m":
            case "M":
                tastenDruck(50);
                break;
            case ",":
                tastenDruck(51);
                break;
            case ".":
                tastenDruck(52);
                break;
            case ":":
                tastenDruck(53);
                break;
            case "0":
                tastenDruck(54);
                break;
            case "+":
                tastenDruck(55);
                break;
            case " ":
                tastenDruck(57);
                break;
        }

        //Die beiden Strg Tasten werden fuer 1... und A... verwendet
        if (Input.GetKeyDown(KeyCode.LeftControl))
            tastenDruck(56);
        if (Input.GetKeyDown(KeyCode.RightControl))
            tastenDruck(58);

        //Zeilentransport und Wagenruecklauf
        if (Input.GetKeyDown(KeyCode.Return))
            tastenDruck(43);
        if (Input.GetKeyDown(KeyCode.Home))
            tastenDruck(28);
    }


    //Erst hier wird die Wirkung des Tastendrucks behandelt und das auch nur wenn der Fernschreiber aktiviert ist
    public void tasteAusloesen (int tastenNr)
    {
        if (modus != Modus.Warten)
        {
            switch (tastenNr)
            {
                case 0:
                    schreiben("[");
                    break;
                case 1:
                    schreiben("]");
                    break;
                case 2:
                    schreiben(";");
                    break;
                case 3:
                    //Andreaskreuz
                    schreiben("?");
                    break;
                case 4:
                    schreiben("/");
                    break;
                case 5:
                    schreiben("'");
                    break;
                case 6:
                    schreiben("=");
                    break;
                case 7:
                    schreiben("(");
                    break;
                case 8:
                    schreiben(")");
                    break;
                case 9:
                    //tiefe 10
                    schreiben("_");
                    break;
                case 10:
                    schreiben("7");
                    break;
                case 11:
                    schreiben("8");
                    break;
                case 12:
                    schreiben("9");
                    break;
                case 13:
                    schreiben("-");
                    break;
                case 14:
                    schreiben("Q");
                    break;
                case 15:
                    schreiben("W");
                    break;
                case 16:
                    schreiben("E");
                    break;
                case 17:
                    schreiben("R");
                    break;
                case 18:
                    schreiben("T");
                    break;
                case 19:
                    schreiben("Z");
                    break;
                case 20:
                    schreiben("U");
                    break;
                case 21:
                    schreiben("I");
                    break;
                case 22:
                    schreiben("O");
                    break;
                case 23:
                    schreiben("P");
                    break;
                case 24:
                    schreiben("4");
                    break;
                case 25:
                    schreiben("5");
                    break;
                case 26:
                    schreiben("6");
                    break;
                case 27:
                    //Wer da?
                    schreiben("!");
                    break;
                case 28:
                    //< wagenruecklauf
                    schreiben("<");
                    break;
                case 29:
                    schreiben("A");
                    break;
                case 30:
                    schreiben("S");
                    break;
                case 31:
                    schreiben("D");
                    break;
                case 32:
                    schreiben("F");
                    break;
                case 33:
                    schreiben("G");
                    break;
                case 34:
                    schreiben("H");
                    break;
                case 35:
                    schreiben("J");
                    break;
                case 36:
                    schreiben("K");
                    break;
                case 37:
                    schreiben("L");
                    break;
                case 38:
                    schreiben("#");
                    break;
                case 39:
                    schreiben("1");
                    break;
                case 40:
                    schreiben("2");
                    break;
                case 41:
                    schreiben("3");
                    break;
                case 42:
                    schreiben("*");
                    break;
                case 43:
                    //Drei Striche symbol, zeilenumbruch (ohne wagenruecklauf)
                    schreiben("§");
                    break;
                case 44:
                    schreiben("Y");
                    break;
                case 45:
                    schreiben("X");
                    break;
                case 46:
                    schreiben("C");
                    break;
                case 47:
                    schreiben("V");
                    break;
                case 48:
                    schreiben("B");
                    break;
                case 49:
                    schreiben("N");
                    break;
                case 50:
                    schreiben("M");
                    break;
                case 51:
                    schreiben(",");
                    break;
                case 52:
                    schreiben(".");
                    break;
                case 53:
                    schreiben(":");
                    break;
                case 54:
                    schreiben("0");
                    break;
                case 55:
                    schreiben("+");
                    break;
                case 56:
                    //Ziffernumschaltzeichen 1...
                    schreiben("$");
                    alphabetEingabe = false;
                    break;
                case 57:
                    schreiben(" ");
                    break;
                case 58:
                    //Buchstabenumschaltzeichen A...
                    schreiben("&");
                    alphabetEingabe = true;
                    break;
            }
        }
    }
}
