using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SPUPTestScriptSimple : MonoBehaviour
{
	//var
	public SerialPortUtility.SerialPortUtilityPro serialPort = null;
	public KeyCode writeSimple = KeyCode.Alpha1;
	public KeyCode writeList = KeyCode.Alpha2;
	public KeyCode writeDictionary = KeyCode.Alpha3;
	public KeyCode writeBinary = KeyCode.Alpha4;

	// Use this for initialization
	void Start () {
        LogConnectedDeviceList();
	}

    // Update is called once per frame
    void Update ()
	{
		if (serialPort != null)
		{
			if (Input.GetKeyDown(writeSimple))
			{
				//send
				serialPort.WriteCRLF("TestData");
			}
			if (Input.GetKeyDown(writeList))
			{
				List<string> dataArray = new List<string>();
				dataArray.Add("AAA");
				dataArray.Add("BBB");
				dataArray.Add("CCC");
				serialPort.Write(dataArray,",","<CR><LF>");
				//AAA,BBB,CCC<CR><LF>
			}
			if (Input.GetKeyDown(writeDictionary))
			{
				Dictionary<string, string> dataArray = new Dictionary<string, string>();
				dataArray.Add("AAA", "BBB");
				dataArray.Add("CCC", "DDD");
				serialPort.Write(dataArray, ",", "<CR><LF>");
				//AAA,BBB,CCC,DDD<CR><LF>
			}
			if (Input.GetKeyDown(writeBinary))
			{
				byte[] bin = new byte[6] { 0x00, 0x01, 0x02, 0x03, 0x04, 0xFF };
				serialPort.Write(bin);
			}

			//for Arduino micro leonardo
			if (Input.GetKeyDown(KeyCode.R))
			{
				serialPort.Close();

				int saveBaudRate = serialPort.BaudRate;
				serialPort.BaudRate = 1200;	//Reset Command
				serialPort.Open();
				//Reset
				serialPort.Close();
				serialPort.BaudRate = saveBaudRate; 
			}
		}
	}

	//Example Read Data : AAA,BBB,CCC,DDD<CR><LF>
	//for List data
	public void ReadComplateList(object data)
	{
		var text = data as List<string>;
		for (int i = 0; i < text.Count; ++i)
			Debug.Log(text[i]);
	}

	//Sensor Example
	public void ReadComplateSensorAB(object data)
	{
		var text = data as List<string>;
		if(text.Count != 4)
			return; //discard

		string[] SensorA = text[1].Split(",".ToCharArray());
		string[] SensorB = text[3].Split(",".ToCharArray());

		Vector3 SensorAv = new Vector3(float.Parse(SensorA[0]), float.Parse(SensorA[1]), float.Parse(SensorA[2]));
		Vector3 SensorBv = new Vector3(float.Parse(SensorB[0]), float.Parse(SensorB[1]), float.Parse(SensorB[2]));
		Debug.Log(SensorAv);
		Debug.Log(SensorBv);
	}

	//for Dictonary data
	public void ReadComplateDictonary(object data)
	{
		var text = data as Dictionary<string, string>;
		foreach (KeyValuePair<string, string> kvp in text)
		{
		  Debug.Log(string.Format("{0}={1}", kvp.Key, kvp.Value));
		}
	}

	//for String data
	public void ReadComplateString(object data)
	{
		var text = data as string;
		Debug.Log(text);
	}

	//for Streaming Binary Data
	public void ReadStreamingBinary(object data)
	{
		var bin = data as byte[];
		string byteArray = System.BitConverter.ToString(bin);
		Debug.Log(byteArray);
	}

	//for System Binary data
	public void ReadComplateProcessing(object data)
	{
		var binData = data as byte[];	//total 14 byte
		string header = System.Text.Encoding.ASCII.GetString(binData, 0, 3);	//Header
		byte[] mainData = new byte[9];	//9byte
		byte[] checkSum = new byte[2];	//2byte

		Array.Copy(binData, 3, mainData, 0, 9);	//main data 3-12 : 9 byte
		Array.Copy(binData, 12, checkSum, 0, 2);	//sum data 12-14 : 2byte

		//processing
		ushort checksumINT = BitConverter.ToUInt16(checkSum,0);	

		// This is heavy!
		//Debug.Log(header);
		//mainData[]
		//Debug.Log(checksumINT);
	}

	//for String data
	public void ReadComplateModbus(object data)
	{
		var mudbus = data as SerialPortUtility.SPUPMudbusData;

		string byteArray = System.BitConverter.ToString(mudbus.Data);
		Debug.Log(string.Format("ADDRESS:{0}, FUNCTION:{1}, DATA:{2}", mudbus.Address, mudbus.Function, byteArray));

		bool isRtuMode = serialPort.ReadProtocol == SerialPortUtility.SerialPortUtilityPro.MethodSystem.ModbusRTU;
		serialPort.Write(mudbus, isRtuMode);	//echo
	}

    //Deviceinfo
    public void LogConnectedDeviceList()
    {
        SerialPortUtility.SerialPortUtilityPro.DeviceInfo[] devicelist =
            SerialPortUtility.SerialPortUtilityPro.GetConnectedDeviceList(serialPort.OpenMethod);

        foreach (SerialPortUtility.SerialPortUtilityPro.DeviceInfo d in devicelist)
        {
            Debug.Log("VendorID:" + d.Vendor + " DeviceName:" + d.SerialNumber);
        }
    }
}
