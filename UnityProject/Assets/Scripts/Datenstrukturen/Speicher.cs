using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[System.Serializable]
public struct Speicher  {

	Wort[] speicher;
	//parityBits[i] ist true wenn speicher[i] eine gerade Zahl Bits hat
	bool[] parityBits;

	//Dient dazu herauszufinden, welche Speicherstellen ungleich 0 sind
	bool[] genutzt;

	bool updated;
	bool alarm;
	public readonly int Length;

	public Speicher (int laenge)
	{
		speicher = new Wort[laenge];
		parityBits = new bool[laenge];
		genutzt = new bool[laenge];
		Length = speicher.Length;
		updated = false;
		alarm = false;
	}


	public Wort this[int stelle]
	{
		get	{return lies(stelle);}
		set {schreib(value, stelle);}
	}


	public Wort this[Wort stelle]
	{
		get	{return lies((int)stelle);}
		set {schreib(value, (int)stelle);}
	}


	public Wort lies(int stelle)
	{
		//Falls versucht wird außerhalb des Speicherbereichs zu lesen
		if (stelle >= Length || stelle < 0)
		{
			return new Wort(0);
		}
		else
		{
			if (parityBits[stelle] != speicher[stelle].getParity())
			{
				alarm = true;
			}

			return speicher[stelle];
		}
	}


	public void schreib(Wort inhalt, int stelle)
	{
		if (stelle < Length)
		{
			speicher[stelle] = inhalt;
			parityBits[stelle] = speicher[stelle].getParity();
			//Wenn 0 geschrieben wird, ist die Zelle nicht mehr genutzt
			genutzt[stelle] = (inhalt != 0);

			updated = true;
		}
	}


	public bool[] getGenutzt()
	{
		return genutzt;
	}


	public bool wasUpdated()
	{
		bool output = updated;
		updated = false;
		return output;
	}


	public bool getAlarm()
	{
		bool ergebnis = alarm;
		alarm = false;
		return ergebnis;
	}
}
