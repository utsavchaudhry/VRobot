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

    private void Start()
    {
        _ = StartCoroutine(GenerateMessage());
    }

    private IEnumerator GenerateMessage()
    {
        bool online = !(FindObjectOfType<SerialHandler>() || FindObjectOfType<SerialCommunicator>() || FindObjectOfType<SerialPortUtilityPro>());
        ServoJoint[] servoJoints = FindObjectsOfType<ServoJoint>().OrderBy(j => j.GetMotorID()).ToArray();
        Clamp[] clamps = FindObjectsOfType<Clamp>();
        ChatApp _chatAppobj = FindObjectOfType<ChatApp>();
        Dictionary<int, int> signals = new();

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
                        //send at last index
                        if (i == servoJoints.Length - 1)
                        {
                            _msg += servoJoints[i].GetCurrentSignal();

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
                            _msg += servoJoints[i].GetCurrentSignal() + ",";
                        }
                    }
                    else
                    {
                        bool send = !signals.ContainsKey(currentID);
                        int signal = servoJoints[i].GetCurrentSignal();

                        if (!send)
                        {
                            send = Mathf.Abs(signal - signals[currentID]) >= 15f;
                        }

                        string command = currentID + "," + signal;

                        if (send && SerialHandler.SendSerialData(command))
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

                    currentID++;
                }
            }

            yield return null;
        }
    }
}
