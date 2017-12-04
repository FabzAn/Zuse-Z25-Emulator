using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class TypenhebelScript : MonoBehaviour {


    public PapierScript papier;
    public GameObject farbband;

	string buffer = "";
	int frameCounter = 0;
	Vector3 ausgangsPosition;

	//Fuer das Leerzeichen macht der Typenhebel keine Bewegung
	bool whitespace = false;
    bool typenhebelBewegen = false;


    public GameObject papierCanvas;
    public Text textPrefab;
    public RawImage werDaPrefab;


    public int papierSpaltenNr = 0;
    public int papierZeilenNr = 0;

    public int papierSpaltenMax;
    public int papierZeilenMax;
    public float zeichenAbstand;
    public float zeilenAbstand;

    public AudioSource tAudioSource;
    public AudioClip[] geraeusche;


	void Start ()
	{
		ausgangsPosition = transform.position;
	}


	void Update ()
	{
		//Falls noch etwas im Puffer ist und sich das Papier nicht bewegt oder der letzte Durchlauf noch nicht beendet
		if ( (papier.inBewegung == 0 && buffer.Length != 0)
				|| frameCounter != 0)
		{
			switch (frameCounter++)
			{
                case 0:
                    //Pruefen ob Zeichen ein whitespace ist, d.h. keine Typenhebel Bewegung
					whitespace = (new []{' ', '<', '§'}.Contains(buffer[0]));
                    //Farbband Rauf auf 0, 24.1, -5.8 (local) in zwei Schritten
                    if (!whitespace)
                    {
                        farbband.transform.localPosition = new Vector3 (0f, 23.5f, -5.45f);
                    }
                    break;
				case 1:
					if (!whitespace)
					{
                        farbband.transform.localPosition = new Vector3 (0f, 24.1f, -5.8f);
						transform.Rotate(new Vector3(-115, 0, 0));
                        playAudio();
					}
					break;
				case 2:
                    //Zeichen uebergeben und aus buffer Array entfernen
					druckAufPapier(buffer.Substring(0, 1));
					buffer = buffer.Substring(1);
					break;
				case 3:
					if (!whitespace)
					{
                        farbband.transform.localPosition = new Vector3 (0f, 23.5f, -5.45f);
						transform.Rotate(new Vector3(115, 0, 0));
					}
					break;
                case 4:
                    //Farbband Runter auf 0, 22.9, -5.1 (local)
                    if (!whitespace)
                    {
                        farbband.transform.localPosition = new Vector3 (0f, 22.9f, -5.1f);
                    }
                    break;
				case 5:
					frameCounter = 0;
                    //Sorgt dafuer, dass der Typenhebel sich nicht zu weit bewegt
					if (typenhebelBewegen)
                    {
                        typenhebelBewegen = false;
						transform.Translate(new Vector3 (-zeichenAbstand, 0, 0));
                    }
					break;
			}
		}
	}


	public void druckAufPapier (string inhalt)
	{
        if (inhalt == "<")
            wagenruecklauf();
        else if (inhalt == "§")
            zeilentransport();
        else
        {
            RectTransform textRect;
            if (inhalt == "!")
            {
                RawImage druckWerDa = Instantiate(werDaPrefab, papierCanvas.transform, false);
                textRect = druckWerDa.GetComponent<RectTransform>();
            }
            else
            {
        		//Zeichen in Szene erstellen und Text zuweisen
        		Text druckText = Instantiate(textPrefab, papierCanvas.transform, false);
        		druckText.text = inhalt;

                //Falls eine tiefe 10 eingegeben wurde (als _ kodiert) wird der Inhalt angepasst
                if (inhalt == "_")
                    druckText.text = "10";

        		//Naechste Zeichen Position bestimmen
        		textRect = druckText.GetComponent<RectTransform>();
            }

    		Vector2 zielPosition = textRect.anchoredPosition;

    		zielPosition.x += zeichenAbstand * papierSpaltenNr;
    		zielPosition.y -= zeilenAbstand * papierZeilenNr;

            //Hier wird die 10 ein Stück tiefer gestellt
            if (inhalt == "_")
                zielPosition.y -= zeilenAbstand * 0.25f;

    		textRect.anchoredPosition = zielPosition;

            if (papierSpaltenNr < papierSpaltenMax)
            {
                typenhebelBewegen = true;
                papierSpaltenNr++;
            }
            //Naechste Zeile, falls die Aktuelle voll ist
            else
            {
                wagenruecklauf();
                zeilentransport();
            }
        }
	}


    public void wagenruecklauf ()
    {
        papierSpaltenNr = 0;
        //Typenhebel wieder nach links bewegen
        transform.position = ausgangsPosition;
    }


    public void zeilentransport ()
    {
        papierZeilenNr++;

        //Blatt bereits voll => naechstes Blatt
        if (papierZeilenNr > papierZeilenMax)
        {
            papierZeilenNr = 0;
            papier = papier.papierEndBewegungVorbereiten();
            papierCanvas = papier.transform.GetChild(0).gameObject;

            //Unity kopiert die Schriftzeichen mit, diese muessen nun geloescht werden
            foreach (Transform child in papierCanvas.transform)
                Destroy (child.gameObject);
        }

        papier.papierBewegungVorbereiten();
    }



    //Das Handbuch impliziert unter "Ablochvorschrift", dass # und * beide nicht abgedruckt werden
	public void einreihen(string inhalt)
	{
        //Falls das Zeichen keines der unsichtbaren Zeichen ist, wird es ausgegeben
        if (!(new []{"$", "&", "*", "#"}.Contains(inhalt)))
		      buffer += inhalt;
	}


    public void einreihen(char inhalt)
    {
        //Falls das Zeichen keines der unsichtbaren Zeichen ist, wird es ausgegeben
        if (!(new []{'$', '&', '*', '#'}.Contains(inhalt)))
            buffer += inhalt;
    }


    void playAudio ()
	{
		tAudioSource.clip = geraeusche[Random.Range(0, geraeusche.Length)];
		tAudioSource.Play();
	}
}
