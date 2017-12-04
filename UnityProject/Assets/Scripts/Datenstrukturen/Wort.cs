using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[System.Serializable]
public struct Wort {

	Int64 wert;


	public Wort (int input)
	{
		wert = input;
	}


	public Wort (Int64 input)
	{
		wert = input;
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


	public bool getParity()
	{
		bool parity = true;
		Int64 zuZaehlen = getRange(0,17);

		//Zaehlen ob inhalt grade oder ungerade Anzahl positiver Bits hat
		while (zuZaehlen != 0)
		{
			if ((zuZaehlen & 1) == 1)
				parity = !parity;

			zuZaehlen >>= 1;
		}
		//True bedeutet: die Zahl ist gerade
		return parity;
	}


	public string getAsString()
	{
		return IntFunktionen.toString(wert);
	}


	public string getAsBinaerString()
	{
		string neuerString = "";
		//Die for Schleife fuegt Lehrzeichen zwischen Operationsteil und Parameterteil ein
		for (int i = 17; i >= 0; i--)
		{
			neuerString += (getStelle(i) ? "1" : "0");

			if (i == 10)
				neuerString += " ";
		}

		return neuerString;
	}


	//Wenn kurz == true werden die Abkuerzungen aus dem Handbuch verwendet, sonst das ausgeschriebene Befehlswort
	public string getAsBefehlString(bool kurz)
	{
		string neuerString = "";

		if (getStelle(17))
			neuerString += "P";
		if (getStelle(16))
			neuerString += "Q";
		if (getStelle(15))
			neuerString += "G";

		//Fuer die lange Schreibweise wird zwischen PQG und dem Rest ein Leerzeichen verwendet
		if (!kurz && (getStelle(17) || getStelle(16) || getStelle(15)))
			neuerString += " ";

		switch (getRange(10, 14) >> 10)
		{
			case 0:
				neuerString += (kurz ? "K" : "Keine Operation ");
				break;
			case 1:
				neuerString += (kurz ? "A" : "Addition ");
				break;
			case 2:
				neuerString += (kurz ? "AA" : "Doppelwortaddition ");
				break;
			case 3:
				neuerString += (kurz ? "B" : "Bringen ");
				break;
			case 4:
				neuerString += (kurz ? "BB" : "Doppelwortbringen ");
				break;
			case 5:
				neuerString += (kurz ? "I" : "Konjunktion ");
				break;
			case 6:
				neuerString += (kurz ? "DI" : "Disjunktion ");
				break;
			case 7:
				neuerString += (kurz ? "CA" : "Konstantenaddition ");
				break;
			case 8:
				neuerString += (kurz ? "CB" : "Konstantenbringen ");
				break;
			case 9:
				neuerString += (kurz ? "CI" : "Konstantenkonjunktion ");
				break;
			case 10:
				neuerString += (kurz ? "CS" : "Konstantensubtraktion ");
				break;
			case 11:
				neuerString += (kurz ? "CT" : "Konj. negative Konstante ");
				break;
			case 12:
				neuerString += (kurz ? "IS" : "Konj. negativer Operand ");
				break;
			case 13:
				neuerString += (kurz ? "NS" : "Negativbringen ");
				break;
			case 14:
				neuerString += (kurz ? "S" : "Subtraktion ");
				break;
			case 15:
				neuerString += (kurz ? "SS" : "Doppelwortsubtraktion ");
				break;
			case 16:
				neuerString += (kurz ? "U" : "Umspeichern ");
				break;
			case 17:
				neuerString += (kurz ? "UU" : "Doppelwortumspeichern ");
				break;
			case 18:
				neuerString += (kurz ? "M" : "Multiplikation ");
				break;
			case 19:
				neuerString += (kurz ? "D" : "Division ");
				break;
			case 20:
				neuerString += (kurz ? "SH" : "Verschiebung ");

				if (getStelle(9))
					neuerString += "L";
				if (getStelle(8))
					neuerString += "R";
				if (getStelle(7))
					neuerString += "V";
				if (getStelle(6))
					neuerString += "W";

				if (!kurz)
					neuerString += " ";

				neuerString += getRange(0,5);
				return neuerString;
			case 21:
				neuerString += (kurz ? "TI" : "Tausche Inhalt ");
				break;
			case 22:
				neuerString += (kurz ? "UT" : "Umspeichertransfer ");
				break;
			case 23:
				neuerString += (kurz ? "BT" : "Bringtransfer ");
				break;
			case 24:
				neuerString += (kurz ? "H" : "AER setzen ");
				break;
			case 25:
				neuerString += (kurz ? "MB" : "Magnetbandoperation ");
				neuerString += "(" + (getRange(7,9) >> 7) + "+" + getRange(0,2) + ")";
				return neuerString;
			case 26:
				if (kurz)
					neuerString += (getStelle(9) ? "Y" : "X");
				else
					neuerString += (getStelle(9) ? "Schaltimpuls " : "Freigabeaktivierung ");
				neuerString += "(" + (getRange(5,6) >> 5) + "+" + getRange(0,4) + ")";
				return neuerString;
			case 27:
				neuerString += (kurz ? "Z" : "Zaehlerladen ");
				break;
			case 28:
				neuerString += (kurz ? "ST" : "Stop ");
				break;
			case 29:
				neuerString += (kurz ? "T" : "Testsprung ");
				break;
			case 30:
				neuerString += (kurz ? "F" : "Sprung mit Notierung ");
				break;
			case 31:
				neuerString += (kurz ? "E" : "Sprungbefehl ");
				break;
		}

		neuerString += getRange(0,9);

		return neuerString;
	}


	public static implicit operator Wort(int input)
	{
		return new Wort(input);
	}


	public static implicit operator Wort(Int64 input)
	{
		return new Wort(input);
	}


	public static implicit operator Int64(Wort input)
	{
		//LinksRechts Shift bringt das Vorzeichen von Stelle 17 auf den Rest des int
		Int64 output = input.wert << 46;
		output >>= 46;
		return output;
	}
}
