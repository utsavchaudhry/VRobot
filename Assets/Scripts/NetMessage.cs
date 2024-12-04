using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Byn.Unity.Examples;
using System.Linq;
using SerialPortUtility;

public class NetMessage : MonoBehaviour
{
    [SerializeField] private Transform signalLogPanel;
    [SerializeField] private GameObject signalLogUiPrefab;
    [SerializeField] private bool log;

    private Dictionary<int, int> signals;
    private ServoJoint[] servoJoints;

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
        Clamp[] clamps = FindObjectsOfType<Clamp>();
        ChatApp _chatAppobj = FindObjectOfType<ChatApp>();
        bool online = !(FindObjectOfType<SerialHandler>() || FindObjectOfType<SerialCommunicator>() || FindObjectOfType<SerialPortUtilityPro>());
        servoJoints = FindObjectsOfType<ServoJoint>().OrderBy(j => j.GetMotorID()).ToArray();
        signals = new();

        while (_chatAppobj || !online)
        {
            if (!VRobot.IsPaused)
            {
                string _msg = string.Empty;
                int currentID = 1;

                for (int i = 0; i < servoJoints.Length; i++)
                {
                    while (currentID < servoJoints[i].GetMotorID())
                    {
                        Clamp clamp = clamps.FirstOrDefault(c => c.GetMotorID() == currentID);
                        if (clamp)
                        {
                            _msg += clamp.GetCurrentSignal();
                        }
                        _msg += ",";
                        currentID++;
                    }

                    if (online)
                    {
                        _msg += servoJoints[i].GetCurrentSignal();

                        //send at last index
                        if (i == servoJoints.Length - 1)
                        {
                            if (_chatAppobj)
                            {
                                _chatAppobj.SendButtonPressed(_msg);
                            }

                            if (log)
                            {
                                Debug.Log(_msg);
                            }
                        }
                        else
                        {
                            _msg += ",";
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
