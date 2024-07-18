using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SPUPTestScriptHTTP : MonoBehaviour
{	
	//var
	public SerialPortUtility.SerialPortUtilityPro serialPort = null;
	private int httpreq = 0;

	// Use this for initialization
	void Start () {}

	void Update()
	{
		if (httpreq == 2)
		{
			//request
			if (serialPort != null)
			{
				System.Threading.Thread.Sleep(10);
				serialPort.WriteCRLF("HTTP/1.1 200 OK");
				serialPort.WriteCRLF("Date: Mon, 04 Feb 2019 09:23:07 GMT");
				serialPort.WriteCRLF("Server: SerialPort Utility Pro");
				serialPort.WriteCRLF("Connection: close");
				serialPort.WriteCRLF("Content-Type: text/html");
				serialPort.WriteCRLF("");
				serialPort.WriteCRLF("<!DOCTYPE html><html><body>Serial Port Utility Pro</body></html>");
				serialPort.WriteCRLF("");

				System.Threading.Thread.Sleep(100);
				serialPort.Close();
				serialPort.Open();
			}
			httpreq = 0;
		}
	}

	//for class
	public void ReadComprateHTTP(object data)
	{
		var httpdat = data as String;
		Debug.Log(httpdat);
		if (httpdat.IndexOf("GET / ") >= 0)
		{
			httpreq++;
		}

		if (httpdat == "")
		{
			httpreq++;
		}
	}

}
