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
                Debug.Log(previousSentData);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    private void Update()
    {
        //Debug.Log(ServoMapper.Instance.GetAngle(ServoMapper.Side.R, ServoMapper.BodyJoint.ShoulderLateral));

        string servoMessage = ServoMapper.Instance.GetAngle(ServoMapper.Side.R, ServoMapper.BodyJoint.Finger).ToString() + "," +
            ServoMapper.Instance.GetAngle(ServoMapper.Side.R, ServoMapper.BodyJoint.Wrist).ToString() + "," +
            ServoMapper.Instance.GetAngle(ServoMapper.Side.R, ServoMapper.BodyJoint.Palm).ToString() + "," +
            ServoMapper.Instance.GetAngle(ServoMapper.Side.R, ServoMapper.BodyJoint.Elbow).ToString() + "," +
            ServoMapper.Instance.GetAngle(ServoMapper.Side.R, ServoMapper.BodyJoint.ShoulderLateral).ToString() + "," +
            ServoMapper.Instance.GetAngle(ServoMapper.Side.R, ServoMapper.BodyJoint.ShoulderForward).ToString() + "," +
            ServoMapper.Instance.GetAngle(ServoMapper.Side.L, ServoMapper.BodyJoint.Finger).ToString() + "," +
            ServoMapper.Instance.GetAngle(ServoMapper.Side.L, ServoMapper.BodyJoint.Wrist).ToString() + "," +
            ServoMapper.Instance.GetAngle(ServoMapper.Side.L, ServoMapper.BodyJoint.Palm).ToString() + "," +
            ServoMapper.Instance.GetAngle(ServoMapper.Side.L, ServoMapper.BodyJoint.Elbow).ToString() + "," +
            ServoMapper.Instance.GetAngle(ServoMapper.Side.L, ServoMapper.BodyJoint.ShoulderLateral).ToString() + "," +
            ServoMapper.Instance.GetAngle(ServoMapper.Side.L, ServoMapper.BodyJoint.ShoulderForward).ToString();

        SendSerialData(servoMessage);

        VRobotSim.Instance.Set(servoMessage);
    }
}
