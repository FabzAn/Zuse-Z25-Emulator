using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipScript : MonoBehaviour {

	public UIHandlingScript myUI;
	public string inhalt;


	bool tooltipBereit = true;
	int frameCounter = 0;
	Vector3 lastMousePosition = new Vector3 (0, 0, 0);


	void Start () {

		if (myUI == null)
			myUI = GameObject.Find("UI Wrapper").GetComponent<UIHandlingScript>();

		if (inhalt == "")
			Debug.Log("Kein Tooltip Text zugewiesen für " + transform.name);

		//Diese Zeile ist noetig, da im Editor keine Zeilenumbrueche eingegeben werden koennen
		inhalt = inhalt.Replace("NEWLINE", "\n");
	}


	void OnMouseEnter()
	{
		if(!EventSystem.current.IsPointerOverGameObject())
		{
			tooltipBereit = true;
		}
	}


	void OnMouseOver()
	{
		if(!EventSystem.current.IsPointerOverGameObject())
		{
			if (tooltipBereit)
			{
				if (lastMousePosition == Input.mousePosition)
				{
					frameCounter++;

					if (frameCounter >= 30)
					{
						myUI.tooltipEin(inhalt);
						tooltipBereit = false;
						frameCounter = 0;
					}
				}
				else
				{
					frameCounter = 0;
					lastMousePosition = Input.mousePosition;
				}
			}
		}
	}


	void OnMouseExit()
	{
		myUI.tooltipAus();
	}
}
