using SerialPortUtility;
using UnityEngine;

public enum MICRO { NOT_FOUND, TC_MOTORS, M_MOTORS, XIAOMI_MOTORS }

public class MicroSerial : MonoBehaviour
{
    public MICRO Micro { get; private set; } = MICRO.NOT_FOUND;

    private SerialPortUtilityPro port;
    private string lastSentMessage;

    private void Start()
    {
        port = GetComponent<SerialPortUtilityPro>();

        if (!port)
        {
            port = FindObjectOfType<SerialPortUtilityPro>();
        }

        if (port)
        {
            port.ReadCompleteEventObject.RemoveAllListeners();
            port.ReadCompleteEventObject.AddListener(SerialMessageReceived);
        }

        Micro = MICRO.NOT_FOUND;
    }

    private void OnDestroy()
    {
        if (port && port.IsOpened())
        {
            port.Close();
        }
    }

    private void SerialMessageReceived(object obj)
    {
        Micro = ((string)obj).Trim().ToLower() switch
        {
            "tc" => MICRO.TC_MOTORS,
            "m" => MICRO.M_MOTORS,
            "xiaomi" => MICRO.XIAOMI_MOTORS,
            _ => Micro
        };
    }

    public bool SendSerialMessage(string msg = "identify")
    {
        if (lastSentMessage != msg && port && port.IsConnected() && port.IsOpened() && port.WriteLF(msg))
        {
            lastSentMessage = msg;
            return true;
        }

        return false;
    }

    private void Update()
    {
        if (Micro == MICRO.NOT_FOUND)
        {
            _ = SendSerialMessage();
        }
    }
}
