using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class FernschreiberTastenScript : MonoBehaviour {

	public FernschreiberScript fernschreiber;

	public AudioClip[] geraeusche;
	AudioSource tastenAudioSource;

	//Ist -1 falls vergessen wurde einen anderen Wert zu setzen
	public int tastenNr = -1;

	public float druckTiefe;
	public float druckTempo;

	Vector3 ausgangsposition;

	bool inBewegung = false;
	bool bewegungRunter = false;

	float startY;


	void Start ()
	{
		if (fernschreiber == null)
			fernschreiber = GameObject.Find("Fernschreiber").GetComponent<FernschreiberScript>();
		if (tastenNr == -1)
			Debug.Log("Keine TastenNr zugewiesen für " +  transform.name);
		if (geraeusche == null)
			Debug.Log("Audio Dateien für Tasten nicht zugewiesen");

		tastenAudioSource = GetComponent<AudioSource>();

		startY = transform.position.y;
		ausgangsposition = transform.position;
	}


	void OnMouseDown ()
	{
		if(!EventSystem.current.IsPointerOverGameObject())
			fernschreiber.tastenDruck(tastenNr);
	}


	public void druecken ()
	{
		//Funktion startet sofort neu, falls Taste bereits gedrueckt wird
		if (inBewegung)
		{
			transform.position = ausgangsposition;
		}

		inBewegung = true;
		bewegungRunter = true;

		//Es wird schon beim Beginn des Tastendrucks ausgeloest. Grund: wenn waehrend des drueckens die Taste nochmal
		//betaetigt wird, wird der Tastdruck abgebrochen und nochmal gedrueckt wenn man nun zum Beispiel
		//100 eingibt, kommt die erste 0 vor der 1 an, weil die Taste 1 noch nicht ganz runter gedrueckt wurde
		//wenn die zweite 0 gedrueckt wird
		fernschreiber.tasteAusloesen(tastenNr);

		tastenAudio();
	}


	void Update ()
	{
		if (inBewegung)
		{
			Vector3 neuePosition = transform.position;

			//Abwaertsbewegung bis zum tiefsten Punkt
			if (bewegungRunter)
			{
				neuePosition.y -= druckTempo * druckTiefe * Time.deltaTime;

				//Taste ist ganz nach unten gedrueckt
				if (neuePosition.y <= startY - druckTiefe)
				{
					bewegungRunter = false;
				}
			}
			//Aufwaertsbewegung bis zur Ausgangsposition
			else
			{
				neuePosition.y += druckTempo * druckTiefe * Time.deltaTime;
				if (neuePosition.y >= startY)
				{
					inBewegung = false;
					//Damit nicht zu weit bewegt wird
					neuePosition.y = startY;
				}
			}

			transform.position = neuePosition;
		}
	}


	void tastenAudio ()
	{
		tastenAudioSource.clip = geraeusche[Random.Range(0, geraeusche.Length)];
		tastenAudioSource.Play();
	}
}
