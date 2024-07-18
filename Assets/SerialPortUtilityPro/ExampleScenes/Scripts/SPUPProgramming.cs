using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SPUPProgramming : MonoBehaviour
{
	//SPUP
	private SerialPortUtility.SerialPortUtilityPro serialPort = null;

	//Config
	public KeyCode SendKeyCode = KeyCode.Alpha1;
	public KeyCode SendKeyCode2 = KeyCode.Alpha2;

	// Use this for initialization
	void Awake()
	{
		//Because processing of plugin carries out by start, it is necessary to generate by Awake. 

		//serialPort = new SerialPortUtility.SerialPortUtilityPro(); //do not use
		serialPort = this.gameObject.AddComponent<SerialPortUtility.SerialPortUtilityPro>();
		
		//config
		serialPort.OpenMethod = SerialPortUtility.SerialPortUtilityPro.OpenSystem.USB;
		serialPort.VendorID = "";
		serialPort.ProductID = "";
		serialPort.SerialNumber = "";
		serialPort.BaudRate = 115200;	//115200kbps

		serialPort.ReadProtocol = SerialPortUtility.SerialPortUtilityPro.MethodSystem.Streaming;
		serialPort.ReadCompleteEventObject.AddListener(this.ReadComprateString);	//read function
		serialPort.RecvDiscardNull = true;

		serialPort.Open();
	}

	void OnDestory()
	{
		if (serialPort != null)
			serialPort.Close();
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (serialPort != null)
		{
			if (serialPort.IsOpened())
			{
				if (Input.GetKeyDown(SendKeyCode))
				{
					byte[] d = System.Text.Encoding.ASCII.GetBytes("test\0\0byte\0\0test");
					serialPort.Write(d);
				}
				if (Input.GetKeyDown(SendKeyCode2))
				{
					serialPort.Write("string data");
				}
			}
		}
	}

	//for 
	public void ReadComprateString(object data)
	{
		var text = data as string;
		Debug.Log(text);
	}
}
