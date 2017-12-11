using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[System.Serializable]
public struct Lochstreifen {
	
	public int[] inhalt { get; set; }

	public string lochstreifenName { get; set; }


	public Lochstreifen (int[] neuerInhalt, string neuerName)
	{
		inhalt = neuerInhalt;
		lochstreifenName = neuerName;
	}


	public Lochstreifen (int[] neuerInhalt)
	{
		inhalt = neuerInhalt;
		lochstreifenName = "Unbenannt";
	}
}
