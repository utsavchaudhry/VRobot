using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SPUPTestScriptJSON : MonoBehaviour
{
	[System.Serializable]
	public class SPAPItem
	{
		public float Right;
		public float Center;
		public float Left;
	}
	
	//var
	public SerialPortUtility.SerialPortUtilityPro serialPort = null;
	public KeyCode writeKey = KeyCode.Alpha1;
	private SPAPItem item = null;

	// Use this for initialization
	void Start () {}
	
	// Update is called once per frame
	void Update ()
	{
		if (serialPort != null)
		{
			if (Input.GetKeyDown(writeKey)) {
				SPAPItem itobj = new SPAPItem();
				itobj.Center = 0.0f;
				itobj.Right = 0.0f;
				itobj.Left = 0.0f;
				serialPort.WriteJSON(itobj);
			}
		}
	}

	//for class
	public void ReadComprateJSON(object data)
	{
		item = data as SPAPItem;

		//Example Read Data : {"Right":1.0,"Center":0.5,"Left":0.0}
		Debug.Log(string.Format("C:{0}, R:{1}, L:{2}",
			item.Center,
			item.Right,
			item.Left));
	}

}
