using UnityEngine;
using SerialPortUtility;

[RequireComponent(typeof(SerialPortUtilityPro))]
public class SerialHandler : MonoBehaviour
{
    private SerialPortUtilityPro serialPort;
    private string previousSentData;

    private void Start()
    {
        serialPort = GetComponent<SerialPortUtilityPro>();

        if (!serialPort)
        {
            serialPort = FindObjectOfType<SerialPortUtilityPro>();
        }
    }

    private void OnDestroy()
    {
        if (serialPort && serialPort.IsOpened())
        {
            serialPort.Close();
        }
    }

    private void SendSerialData(string serialMessage)
    {
        if (!serialPort.IsConnected())
        {
            Debug.Log("Serial port not connected!");
            return;
        }

        if (!serialPort.IsOpened())
        {
            Debug.LogError("Serial port not opened!");
            return;
        }

        if (serialMessage == previousSentData)
        {
            return;
        }

        try
        {
            if (serialPort.WriteLF(serialMessage))
            {
                previousSentData = serialMessage;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    private void Update()
    {
        if (ServoMapper.Instance)
        {
            SendSerialData(ServoMapper.Instance.GetServoMessage());
        }
    }
}
