using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public class SPUPDeviceList : MonoBehaviour
{
	//SPUP
    List<SerialPortUtility.SerialPortUtilityPro> spList = new List<SerialPortUtility.SerialPortUtilityPro>();
    public int baudrate = 115200;
    public SerialPortUtility.SerialPortUtilityPro.OpenSystem openMode = SerialPortUtility.SerialPortUtilityPro.OpenSystem.USB;
    public UnityEngine.UI.Text nodeviceText = null;

    // Use this for initialization
    void Awake()
	{
        //Because processing of plugin carries out by start, it is necessary to generate by Awake. 

        SerialPortUtility.SerialPortUtilityPro.DeviceInfo[] devicelist = 
            SerialPortUtility.SerialPortUtilityPro.GetConnectedDeviceList(openMode);

        if (devicelist == null)
            return;

        foreach (SerialPortUtility.SerialPortUtilityPro.DeviceInfo d in devicelist)
        {
            //serialPort = new SerialPortUtility.SerialPortUtilityPro(); //do not use
            String GameObjectName = "NONE";
            if (openMode == SerialPortUtility.SerialPortUtilityPro.OpenSystem.USB)
                GameObjectName = "VID:" + d.Vendor + ", PID:" + d.Product;
            else if (openMode == SerialPortUtility.SerialPortUtilityPro.OpenSystem.BluetoothSSP)
                GameObjectName = d.SerialNumber;
            else if (openMode == SerialPortUtility.SerialPortUtilityPro.OpenSystem.PCI)
                GameObjectName = d.Vendor;
            GameObject obj = new GameObject(GameObjectName);
            SerialPortUtility.SerialPortUtilityPro serialPort = obj.AddComponent<SerialPortUtility.SerialPortUtilityPro>();
            //Debug.Log(d.PortName);    //Get PortName
            
            //config
            serialPort.SetDebugConsoleMonitorView(true);

            serialPort.OpenMethod = openMode;
            serialPort.VendorID = d.Vendor;
            serialPort.ProductID = d.Product;
            serialPort.SerialNumber = d.SerialNumber;
            serialPort.BaudRate = baudrate;   //115200kbps
            //serialPort.Skip = 1;

            serialPort.ReadProtocol = SerialPortUtility.SerialPortUtilityPro.MethodSystem.Streaming;
            serialPort.RecvDiscardNull = true;

            serialPort.IsAutoOpen = false;
            spList.Add(serialPort);
            nodeviceText.enabled = false;
        }
        
	}

	void OnDestory()
	{
        foreach (SerialPortUtility.SerialPortUtilityPro item in spList)
        {
            item.Close();
        }
	}
	
	// Update is called once per frame
	void Update ()
	{
        if (Input.GetKey(KeyCode.Home) || Input.GetKey(KeyCode.Escape) || Input.GetKey(KeyCode.Menu))
        {
            Application.runInBackground = false;
            Application.Quit();
            return;
        }
    }

	//for 
	public void ReadComprateString(object data)
	{
		var text = data as string;
		Debug.Log(text);
	}

    public void OnButtonRefresh()
    {
        SceneManager.LoadScene (SceneManager.GetActiveScene().name);
    }
}
