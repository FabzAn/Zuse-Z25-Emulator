using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public struct Doppelwort {

	Int64 wert;


	public Doppelwort (int input)
	{
		wert = input;
	}


	public Doppelwort (Int64 input)
	{
		wert = input;
	}


	public Doppelwort (Wort unteresWort, Wort oberesWort)
	{
		Int64 obererWert = oberesWort.getRange(0,17);
		Int64 untererWert = unteresWort.getRange(0,17);

		wert = obererWert << 18;
		wert |= untererWert;

		//Vorzeichen auf den ganzen Integer propagieren
		wert <<= 28;
		wert >>= 28;
	}


	public bool this[int stelle]
	{
		get	{return getStelle(stelle);}
		set {setStelle(stelle, value);}
	}


	public bool getStelle(int stelle)
	{
		return IntFunktionen.getStelle(wert, stelle);
	}


	public void setStelle(int stelle, bool inhalt)
	{
		wert = IntFunktionen.setStelle(wert, stelle, inhalt);
	}


	//Start und Ende sind inklusiv
	public Int64 getRange(int startStelle, int endStelle)
	{
		return IntFunktionen.getRange(wert, startStelle, endStelle);
	}


	public Wort getUnteresWort()
	{
		return new Wort(getRange(0, 17));
	}


	public Wort getOberesWort()
	{
		return new Wort(getRange(18, 35) >> 18);
	}


	public string getAsString()
	{
		return IntFunktionen.toString(wert);
	}


	public static implicit operator Doppelwort(Int64 input)
	{
		return new Doppelwort(input);
	}


	public static implicit operator Int64(Doppelwort input)
	{
		//Vorzeichen korrigieren
		Int64 output = input.wert << 28;
		output >>= 28;
		return output;
	}
}
