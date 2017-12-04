using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct Speichereinheit
{

	Wort[,] magnetTrommel;
	Speicher rechenWerkSpeicher;

	public Speichereinheit (Wort[,] trommel, Speicher speicher)
	{
		magnetTrommel = trommel;
		rechenWerkSpeicher = speicher;
	}


	public Wort[,] getTrommel ()
	{
		return magnetTrommel;
	}


	public Speicher getSpeicher ()
	{
		return rechenWerkSpeicher;
	}
}
