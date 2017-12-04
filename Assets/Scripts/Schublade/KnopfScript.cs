using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class KnopfScript : MonoBehaviour {

	public RechenwerkScript rechenWerk;
	public Light oberesLicht;
	public Light unteresLicht;


	[Tooltip("Große Textur hierhin")]
	public Texture großeTextur;
	[Tooltip("Kleine Textur hierhin")]
	public Texture kleineTextur;

	[Tooltip("Gewuenschte Knopfgeraeusche hier referenzieren")]
	public AudioClip[] geraeusche;
	AudioSource knopfAudioSource;

	//Ist -1 falls vergessen wurde einen anderen Wert zu setzen
	[Tooltip("Angabe um welchen Knopf es sich handelt")]
	public int knopfNr = -1;
	[Tooltip("Distanz, die der Knopf eingdrueckt wird")]
	public float druckTiefe;
	[Tooltip("Tempo mit dem Knopf sich bewegt, in Einheiten pro Sekunde")]
	public float druckTempo;


	//Um sicherzustellen, dass Knopf nicht mehrmals auf einmal gedrueckt wird
	private bool inBewegung = false;
	private bool bewegungRunter = false;

	private float startY;


	void Start ()
	{
		if (rechenWerk == null)
			Debug.Log("rechenWerk nicht zugewiesen für: " + transform.name);

		if (oberesLicht == null)
			Debug.Log("oberesLicht nicht zugewiesen für: " + transform.name);

		if (unteresLicht == null)
			Debug.Log("unteresLicht nicht zugewiesen für: " + transform.name);

		if (geraeusche == null)
			Debug.Log("Audio Dateien für Knöpfe nicht zugewiesen");

		knopfAudioSource = GetComponent<AudioSource>();

		startY = transform.position.y;
	}


	void OnMouseDown ()
	{
		if(!EventSystem.current.IsPointerOverGameObject())
		{
			//Knopf kann nur gedrueckt werden, wenn er nicht schon gedrueckt wird
			if (!inBewegung)
			{
				inBewegung = true;
				bewegungRunter = true;

				knopfAudio();
			}
		}
	}


	void Update ()
	{
		if (inBewegung)
		{
			Vector3 neuePosition = this.transform.position;

			//Abwaertsbewegung bis zum tiefsten Punkt
			if (bewegungRunter)
			{
				neuePosition.y -= druckTempo * druckTiefe * Time.deltaTime;

				//Knopf ist ganz nach unten gedrueckt
				if (neuePosition.y <= startY - druckTiefe)
				{
					rechenWerk.knopfCall(knopfNr);
					bewegungRunter = false;
				}
			}
			//Aufwaertsbewegung bis zur Ausgangsposition
			else
			{
				neuePosition.y += druckTempo * druckTiefe * Time.deltaTime;
				if (neuePosition.y >= startY)
					inBewegung = false;
			}

			transform.position = neuePosition;
		}
	}


	public void setOberesLicht (bool neuerWert)
	{
		oberesLicht.enabled = neuerWert;
	}


	public void setUnteresLicht (bool neuerWert)
	{
		unteresLicht.enabled = neuerWert;
	}


	public void switchTexture (bool input)
	{
		//input == true bedeutet, die vergroesserten Texturen werden benutzt
		if (input)
			GetComponent<Renderer>().material.SetTexture("_MainTex", großeTextur);
		else
			GetComponent<Renderer>().material.SetTexture("_MainTex", kleineTextur);
	}


	void knopfAudio ()
	{
		knopfAudioSource.clip = geraeusche[Random.Range(0, geraeusche.Length)];
		knopfAudioSource.Play();
	}
}
