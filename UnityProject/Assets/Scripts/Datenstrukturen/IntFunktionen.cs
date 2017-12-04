using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class IntFunktionen {

	//Die binaere Representation wird ausgegeben
	public static string toString(Int64 value)
    {
		return Convert.ToString(value, 2);
    }


	public static Int64 shiftMaske(int stelle)
	{
		//Workaround da jedes mal nur 18 oder 19 bit weit geshifted wird
		int verschiebeWert = stelle;
		bool zuViel = true;
		Int64 shiftedBit = 1;

		while (zuViel)
		{
			if (stelle > 18)
			{
				verschiebeWert = 18;
				stelle -= 18;
			}
			else
			{
				zuViel = false;
				verschiebeWert = stelle;
			}
			shiftedBit <<= verschiebeWert;
		}
		return shiftedBit;
	}


	public static bool getStelle(Int64 wert, int stelle)
	{
		Int64 shiftedBit = shiftMaske(stelle) & wert;
		return shiftedBit != 0 ? true : false;
	}


	public static Int64 setStelle(Int64 wert, int stelle, bool inhalt)
	{
		Int64 shiftedBit = shiftMaske(stelle);
		if (inhalt)
			wert |= shiftedBit;
		else
			wert &= ~shiftedBit;
		return wert;
	}


	public static int setStelle(int wert, int stelle, bool inhalt)
	{
		return (int) setStelle((long) wert, stelle, inhalt);
	}


	//Start und Ende sind inklusiv. Beispiel:
	//1111010100 mit getRange (3, 7) gibt
	//0011010000
	public static Int64 getRange(Int64 wert, int startStelle, int endStelle)
	{
		if (startStelle > endStelle)
			return 0;

		Int64 shiftedBit = 1, output = 0;

		for (int i = 0; i <= endStelle; i++)
		{
			if (i >= startStelle)
				output |= shiftedBit;
			shiftedBit <<= 1;
		}
		return output & wert;
	}


	public static int getRange(int wert, int startStelle, int endStelle)
	{
		return (int) getRange((long) wert, startStelle, endStelle);
	}
}
