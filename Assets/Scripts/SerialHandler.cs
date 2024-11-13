using UnityEngine;
using System.Collections;
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

        _ = StartCoroutine(SendSignal());
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
            return;
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

    private IEnumerator SendSignal()
    {
        while (true)
        {
            if (!VRobot.IsPaused)
            {
                if (ServoMapper.Instance)
                {
                    if (ServoMapper.Instance.IsReady)
                    {
                        SendSerialData(ServoMapper.Instance.GetServoMessage());
                    }
                }
                else
                {
                    SendSerialData(SignalGenerator.Signal);
                }
            }

            yield return null;
        }
    }
}
