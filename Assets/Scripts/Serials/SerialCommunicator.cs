using UnityEngine;
using System.IO.Ports;
using System.Collections;

public class SerialCommunicator : MonoBehaviour
{
    [SerializeField]
    private int baudRate = 115200; // The baud rate can be changed in the Unity Inspector.

    private static SerialPort serialPort;
    private static SerialCommunicator instance;

    void Awake()
    {
        // Ensure only one instance exists.
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            StartCoroutine(InitializeSerialPort());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Initializes the serial port by auto-detecting the ESP32 port.
    /// </summary>
    IEnumerator InitializeSerialPort()
    {
        while (serialPort == null || !serialPort.IsOpen)
        {
            string portName = FindESP32Port();
            if (!string.IsNullOrEmpty(portName))
            {
                try
                {
                    serialPort = new SerialPort(portName, baudRate);
                    serialPort.Open();
                    Debug.Log($"Connected to ESP32 on port {portName}");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to open port {portName}: {e.Message}");
                }
            }
            else
            {
                Debug.Log("ESP32 port not found. Retrying in 1 second...");
            }
            yield return new WaitForSeconds(1f);
        }
    }

    /// <summary>
    /// Finds the serial port connected to the ESP32.
    /// </summary>
    /// <returns>The port name if found; otherwise, null.</returns>
    private string FindESP32Port()
    {
        string[] ports = SerialPort.GetPortNames();
        foreach (string port in ports)
        {
            // Common identifiers for ESP32 ports.
            if (port.Contains("ttyUSB") || port.Contains("ttyACM") || port.Contains("cu.SLAB_USBtoUART") || port.Contains("COM"))
            {
                return port;
            }
        }
        return null;
    }

    /// <summary>
    /// Writes a message to the serial port connected to the ESP32.
    /// </summary>
    /// <param name="message">The message to send.</param>
    public static void WriteToSerial(string message)
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            try
            {
                serialPort.WriteLine(message);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to write to serial port: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("Serial port is not open or initialized.");
        }
    }

    void OnDestroy()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
        }
    }
}
