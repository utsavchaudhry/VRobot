using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SPUPTestScriptGObj : MonoBehaviour
{
	//Item
	public string Item1;
	public string Item2;
	public string Item3;

	// Use this for initialization
	void Start ()
	{
		Item1 = "";
		Item2 = "";
		Item3 = "";
	}
	
	// Update is called once per frame
	void Update ()
	{
		//Example Read Data : SPUPTestScriptGObj.Item1,TESTDATA,SPUPTestScriptGObj.Item2,TESTDATA2,SPUPTestScriptGObj.Item3,TESTDATA3<CR><LF>
		//These display content of data by a debugging log. 
		if(Item1 != string.Empty)
			Debug.Log(Item1);

		if (Item1 != string.Empty)
			Debug.Log(Item1);

		if (Item1 != string.Empty)
			Debug.Log(Item1);
	}
}
