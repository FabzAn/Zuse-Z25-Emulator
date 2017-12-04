using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FernschreiberKippschalterScript : MonoBehaviour {

	public FernschreiberScript fernschreiber;
	public int kippschalterNr = -1;
	public float kippGeschwindigkeit;

	public AudioClip[] geraeusche;
	AudioSource kippSchalterAudioSource;


	Quaternion ausgangsWinkel = Quaternion.Euler(270, -90, 0);
	Quaternion zielWinkel = Quaternion.Euler(250, -90, 0);
	Quaternion vergleichsWinkel;
	bool inBewegung = false;
	bool aktiviert = false;


	void Start ()
	{
		if (kippschalterNr == -1)
			Debug.Log("kippschalterNr für " + transform.name + " nicht zugewiesen.");
		if (fernschreiber == null)
			fernschreiber = GameObject.Find("Körper").GetComponent<FernschreiberScript>();

		kippSchalterAudioSource = GetComponent<AudioSource>();
	}


	void Update ()
	{
		if (inBewegung)
		{
			transform.rotation = Quaternion.RotateTowards(transform.rotation, vergleichsWinkel, 20 * Time.deltaTime * kippGeschwindigkeit);

			if (transform.rotation == vergleichsWinkel)
			{
				fernschreiber.kippschalterUmlegen(kippschalterNr, aktiviert);
				inBewegung = false;
			}
		}
	}


	void OnMouseDown ()
	{
		if (!inBewegung)
		{
			inBewegung = true;
			aktiviert = !aktiviert;
			vergleichsWinkel = aktiviert ? zielWinkel : ausgangsWinkel;
			audioAn();
		}
	}


	void audioAn ()
	{
		kippSchalterAudioSource.clip = geraeusche[Random.Range(0, geraeusche.Length)];
		kippSchalterAudioSource.Play();
	}
}
