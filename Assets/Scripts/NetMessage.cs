using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Byn.Unity.Examples;
using System.Linq;
using SerialPortUtility;
using System.Text;

public class NetMessage : MonoBehaviour
{
    [SerializeField] private Transform signalLogPanel;
    [SerializeField] private GameObject signalLogUiPrefab;
    [SerializeField] private bool log;

    private Dictionary<int, int> signals;

    private void Start()
    {
        _ = StartCoroutine(GenerateMessage());
    }

    private void SendSerialCommand(int currentID, int signal)
    {
        if (signals.ContainsKey(currentID) && Mathf.Abs(signal - signals[currentID]) < 15f)
        {
            return;
        }

        string command = currentID + "," + signal;

        if (SerialHandler.SendSerialData(command))
        {
            if (log)
            {
                Debug.Log(command);
            }

            if (!signals.TryAdd(currentID, signal))
            {
                signals[currentID] = signal;
            }

            if (signalLogPanel && signalLogUiPrefab)
            {
                while (currentID > signalLogPanel.childCount)
                {
                    _ = Instantiate(signalLogUiPrefab, signalLogPanel);
                }

                signalLogPanel.GetChild(currentID - 1).GetComponent<SignalLog>().SetPosition(signal.ToString());
            }
        }
    }

    private IEnumerator GenerateMessage()
    {
        ServoJoint[] servoJoints = FindObjectsOfType<ServoJoint>().OrderBy(j => j.GetMotorID()).ToArray();
        Clamp[] clamps = FindObjectsOfType<Clamp>();
        ConferenceApp conference = FindObjectOfType<ConferenceApp>();
        XRJoystickDifferentialDrive differentialDrive = FindObjectOfType<XRJoystickDifferentialDrive>();
        TargetMover targetMover = FindObjectOfType<TargetMover>();
        bool online = !(FindObjectOfType<SerialHandler>() || FindObjectOfType<SerialManager>() || FindObjectOfType<SerialPortUtilityPro>());
        signals = new();

        while (conference || !online)
        {
            if (!VRobot.IsPaused)
            {
                StringBuilder _msg = new();
                StringBuilder _logmsg = new();
                int currentID = 1;

                for (int i = 0; i < servoJoints.Length; i++)
                {
                    while (currentID < servoJoints[i].GetMotorID())
                    {
                        Clamp clamp = clamps.FirstOrDefault(c => c.GetMotorID() == currentID);
                        if (clamp)
                        {
                            _ = _msg.Append(clamp.GetCurrentSignal());
                            _ = _logmsg.Append(clamp.GetCurrentInput().ToString("F1"));
                            _ = _logmsg.Append(",");
                        }
                        _ = _msg.Append(",");
                        currentID++;
                    }

                    if (online)
                    {
                        _ = _msg.Append(servoJoints[i].GetCurrentSignal());
                        _ = _logmsg.Append(servoJoints[i].GetCurrentAngle());

                        //send at last index
                        if (i == servoJoints.Length - 1)
                        {
                            if (conference && conference.IsActiveClient())
                            {
                                _ = _msg.Append(",f,");
                                _ = _msg.Append(JoystickFingerSignalGenerator.Signal);
                                _ = _msg.Append(",");
                                _ = _logmsg.Append(",");
                                if (differentialDrive)
                                {
                                    _ = _msg.Append(differentialDrive.LeftWheelSpeed.ToString("F1"));
                                    _ = _logmsg.Append(differentialDrive.LeftWheelSpeed.ToString("F1"));

                                    _ = _msg.Append(",");
                                    _ = _logmsg.Append(",");

                                    _ = _msg.Append(differentialDrive.RightWheelSpeed.ToString("F1"));
                                    _ = _logmsg.Append(differentialDrive.RightWheelSpeed.ToString("F1"));
                                }
                                else if (targetMover)
                                {
                                    _ = _msg.Append(targetMover.LeftWheelSpeed.ToString("F1"));
                                    _ = _logmsg.Append(targetMover.LeftWheelSpeed.ToString("F1"));

                                    _ = _msg.Append(",");
                                    _ = _logmsg.Append(",");

                                    _ = _msg.Append(targetMover.RightWheelSpeed.ToString("F1"));
                                    _ = _logmsg.Append(targetMover.RightWheelSpeed.ToString("F1"));
                                }
                                conference.SendMsg(_msg.ToString());
                                DataLoggerManager.SaveSignal(_logmsg.ToString());
                            }

                            if (log)
                            {
                                Debug.Log(_msg);
                            }
                        }
                        else
                        {
                            _ = _msg.Append(",");
                            _ = _logmsg.Append(",");
                        }
                    }
                    else
                    {
                        SendSerialCommand(currentID, servoJoints[i].GetCurrentSignal());
                    }

                    currentID++;
                }

                if (!online)
                {
                    for (int i = 0; i < clamps.Length; i++)
                    {
                        SendSerialCommand(clamps[i].GetMotorID(), clamps[i].GetCurrentSignal());
                    }
                }
            }

            yield return null;
        }
    }
}
