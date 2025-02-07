using UnityEngine;
using SerialPortUtility;

[RequireComponent(typeof(SerialPortUtilityPro))]
public class SerialHandlerWheels : MonoBehaviour
{
    private static SerialPortUtilityPro serialPort;
    private static string previousSentData;

    private void Start()
    {
        serialPort = GetComponent<SerialPortUtilityPro>();

        if (!serialPort)
        {
            serialPort = FindObjectOfType<SerialPortUtilityPro>();
        }

        previousSentData = string.Empty;
    }

    private void OnDestroy()
    {
        if (serialPort && serialPort.IsOpened())
        {
            serialPort.Close();
        }
    }

    public static bool SendSerialData(string serialMessage)
    {
        if (!serialPort || !serialPort.IsConnected() || previousSentData == serialMessage)
        {
            return false;
        }

        if (!serialPort.IsOpened())
        {
            if (string.IsNullOrEmpty(serialPort.Port))
            {
                Debug.LogError("Serial port not opened!");
            }
            else if (!serialPort.IsOpenProcessing())
            {
                try
                {
                    serialPort.Open();
                }
                catch (System.Exception e)
                {
                    Debug.LogError(e.Message);
                }
            }
            return false;
        }

        try
        {
            if (serialPort.WriteLF(serialMessage))
            {
                previousSentData = serialMessage;
                return true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.Message);
        }

        return false;
    }
}
