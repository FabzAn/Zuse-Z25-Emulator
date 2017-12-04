using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PeripherieScript : MonoBehaviour {


	public PeripherieDatenScript peripherie;
	public RechenwerkScript rechenwerk;

	protected Wort speicherAdresse = 0;
	protected bool bereit = true;


	void Start ()
	{
		if (peripherie == null)
			peripherie = GetComponent<PeripherieDatenScript>();
	}


	public virtual void maschineAus()
	{
		reset();
	}


	public virtual void reset()
	{
		Debug.Log("reset an " + transform.name);
	}


	public virtual void schaltimpuls(int i)
	{
		if (bereit)
			Debug.Log("Schaltimpuls an " + transform.name + " mit Parameter: " + i);
	}


	public virtual void bringen ()
	{
		rechenwerk.bringenCallBack(speicherAdresse);
	}


	public virtual void umspeichern(Wort wort)
	{
		if (bereit)
			speicherAdresse = wort;
	}


	public virtual void freigabeaktivierung()
	{
		if (bereit)
			peripherie.freigabe = true;
	}


	public virtual void umspeichertransfer(Wort[] inhalt)
	{
		if (bereit)
			Debug.Log("Umspeichertransfer an " + transform.name);
	}


	public virtual void bringTransfer(int anzahl)
	{
		if (bereit)
			Debug.Log("Bringtransfer an " + transform.name);
	}
}
