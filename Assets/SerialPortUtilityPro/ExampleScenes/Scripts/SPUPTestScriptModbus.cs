using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SPUPTestScriptModbus : MonoBehaviour
{
	//var
	public SerialPortUtility.SerialPortUtilityPro serialPort = null;
	public KeyCode writeBinaryModbus = KeyCode.Alpha1;
	public KeyCode writeBinaryModbusASCII = KeyCode.Alpha2;


	// Use this for initialization
	void Start () {
		LogConnectedDeviceList();
	}

	// Update is called once per frame
	void Update ()
	{
		if (serialPort != null)
		{
			if (Input.GetKeyDown(writeBinaryModbus))
			{
				var mudbus = new SerialPortUtility.SPUPMudbusData(4);

				mudbus.Address = 0x01;
				mudbus.Function = 0x01;
				mudbus.Data[0] = 0x00;
				mudbus.Data[1] = 0x00;
				mudbus.Data[2] = 0x00;
				mudbus.Data[3] = 0x08;
				serialPort.Write(mudbus, true);
			}

			if (Input.GetKeyDown(writeBinaryModbusASCII))
			{
				var mudbus = new SerialPortUtility.SPUPMudbusData(4);

				mudbus.Address = 0x01;
				mudbus.Function = 0x01;
				mudbus.Data[0] = 0x00;
				mudbus.Data[1] = 0x00;
				mudbus.Data[2] = 0x00;
				mudbus.Data[3] = 0x08;
				serialPort.Write(mudbus, false);
			}
		}
	}

	//for String data
	public void ReadComplateModbus(object data)
	{
		var mudbus = data as SerialPortUtility.SPUPMudbusData;

		string byteArray = System.BitConverter.ToString(mudbus.Data);
		Debug.Log(string.Format("ADDRESS:{0}, FUNCTION:{1}, DATA:{2}", mudbus.Address, mudbus.Function, byteArray));
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
