using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PapierScript : MonoBehaviour {

	Vector3 papierverschiebung;
	public float papierGeschwindigkeit;
	public int inBewegung = 0;
    public int endBewegungPhase = 0;

	public Transform papierDummy;
	Transform papierDummyPointer;
	public Transform neuesPapier;
	Transform neuesPapierPointer;

	Vector3 startPosition = new Vector3 (-6.24f, 69f, 55.27f);
	public Vector3 neuePosition;

	Vector3 rueckVector;
	float rotationsTimer = 0;


	void Start ()
	{
		papierverschiebung = -transform.up.normalized * 1.5f;
	}


	void Update ()
	{
		if (inBewegung > 0)
		{
            switch (endBewegungPhase)
            {
                //Fall 0 bedeutet, dass eine ganz normale Verschiebung des Papiers durchgefuehrt wird,
                //Die Faelle groesser 0 sind die Phasen der Verschiebung des Papiers auf die Ablage
                case 0:
        			transform.position += papierverschiebung * papierGeschwindigkeit * Time.deltaTime;

        			//A ist altePosi B ist neuePosi papierverschiebung entspricht A->B rueckVector entspricht B+bisherVerschoben->A
        			//Wenn noch nicht verschoben wurde ist rueckVector = -papierverschiebung
        			//Wenn papierverschiebung und rueckVector die selben vorzeichen haben wurde über B hinausgeschoben
        			rueckVector = transform.position - neuePosition;
        			//Das if statement ueberprueft ob die Vorzeichen gleich sind
        			if (rueckVector.x*papierverschiebung.x >= 0
        				&& rueckVector.y*papierverschiebung.y >= 0
        				&& rueckVector.z*papierverschiebung.z >= 0)
        			{
        				transform.position = neuePosition;
        				inBewegung--;
                        neuePosition = transform.position + papierverschiebung;
        			}
                    break;

                case 1:
                    //Papierdummy von papierEndBewegungVorbereiten() instantiiert
					//Der Papierdummy wird benutzt, weil das richtige Papier zu lang ist und durch den Tisch clippen wuerde
                    //Beide Papiere bewegen bis erstes Papier nach oben rausgeschoben, Papierdummy ist an Papier ausgangsposition
					transform.position += papierverschiebung * papierGeschwindigkeit * Time.deltaTime;
					papierDummyPointer.transform.position += papierverschiebung * papierGeschwindigkeit * Time.deltaTime;

					rueckVector = papierDummyPointer.transform.position - startPosition;

					if (rueckVector.x*papierverschiebung.x >= 0
        				&& rueckVector.y*papierverschiebung.y >= 0
        				&& rueckVector.z*papierverschiebung.z >= 0)
					{
						endBewegungPhase++;
					}
                    break;
                case 2:
                    //Papier um untere linke Ecke (-12.9602, 16.0367, 0) rotieren ~10°
					transform.RotateAround(transform.TransformPoint(new Vector3(-12.9602f, 16.0367f, 0f)), transform.forward, -10 * Time.deltaTime);
					rotationsTimer += Time.deltaTime;
					if (rotationsTimer >= 1)
					{
						endBewegungPhase++;
						rotationsTimer = 0;
					}
                    break;
                case 3:
                    //Papier um untere rechte Ecke rotieren bis wieder wagerecht
					transform.RotateAround(transform.TransformPoint(new Vector3(12.9602f, 16.0367f, 0f)), transform.forward, 10 * Time.deltaTime);
					rotationsTimer += Time.deltaTime;
					if (rotationsTimer >= 1)
					{
						endBewegungPhase++;
					}
                    break;
                case 4:
                    //Papier seitwaerts bewegen bis auf Hoehe der Ablage (z == 22.4)
					Vector3 linksVerschiebung = transform.position;
					linksVerschiebung.z -= 30 * Time.deltaTime;
					transform.position = linksVerschiebung;
					if (transform.position.z <= 22.4)
					{
						endBewegungPhase++;
					}
                    break;
                case 5:
                    //Papier nach hinten ueber Ablage (x == 39.9) bewegen und dabei rotieren, so dass Schrift nach unten zeigt
					Vector3 rueckVerschiebung = transform.position;
					rueckVerschiebung.x += 30 * Time.deltaTime;
					transform.position = rueckVerschiebung;

					transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(-270, 90, 0), 118 * Time.deltaTime);

					if (transform.position.x >= 39.9)
					{
						//Papierdummy durch richtiges Papier ersetzen
						neuesPapierPointer.transform.position = papierDummyPointer.transform.position;
						neuesPapierPointer.GetComponent<PapierScript>().inBewegung = 0;

						Destroy(papierDummyPointer.gameObject);

						endBewegungPhase++;
					}
                    break;
                case 6:
                    //Papier nach unten bewegen bis es nicht mehr zu sehen (y == 55.6) ist und es dann zerstoeren
					Vector3 runterVerschiebung = transform.position;
					runterVerschiebung.y -= 20 * Time.deltaTime;
					transform.position = runterVerschiebung;

					if (transform.position.y <= 55.6)
					{
						Destroy(this.gameObject);
					}
                    break;
            }
		}
	}


    public void papierBewegungVorbereiten ()
    {
        if (inBewegung == 0) neuePosition = transform.position + papierverschiebung;
        inBewegung++;
    }


	public PapierScript papierEndBewegungVorbereiten ()
	{
		papierDummyPointer = papierDummy;
		papierDummyPointer = Instantiate (papierDummyPointer, new Vector3 (-8.59f, 64.68f, 55.27f), Quaternion.Euler(new Vector3 (-152, 90, 180)));
		//neuesPapier wird schon jetzt ausser Sicht instantiiert
		neuesPapierPointer = neuesPapier;
		neuesPapierPointer = Instantiate(neuesPapierPointer, new Vector3 (0f, -100f, 0f), Quaternion.Euler(new Vector3 (-152, 90, 0)));

		PapierScript neuesScript = neuesPapierPointer.GetComponent<PapierScript>();

		//Damit der Fernschreiber noch nicht zugreifen kann
		neuesScript.inBewegung = 1;
		endBewegungPhase = 1;
		inBewegung = 1;
		return neuesScript;
	}
}
