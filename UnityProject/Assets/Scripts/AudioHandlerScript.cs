using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioHandlerScript : MonoBehaviour {

	public AudioSource start;
	public AudioSource mittelteil;
	public AudioSource schluss;

	public AudioSource trommelStart;
	public AudioSource trommelMittelteil;
	public AudioSource trommelSchluss;


	public void maschineStartet ()
	{
		start.Play();
		schluss.Stop();
		mittelteil.PlayDelayed(4.5f);
	}


	public void maschineStoppt ()
	{
		if (start.isPlaying || mittelteil.isPlaying)
			schluss.Play();
		start.Stop();
		mittelteil.Stop();
	}


	public void trommelStarten ()
	{
		trommelStart.Play();
		trommelSchluss.Stop();
		trommelMittelteil.PlayDelayed(30f);
	}


	public void trommelStopp ()
	{
		if (trommelStart.isPlaying || trommelMittelteil.isPlaying)
			trommelSchluss.Play();
		trommelStart.Stop();
		trommelMittelteil.Stop();
	}
}
