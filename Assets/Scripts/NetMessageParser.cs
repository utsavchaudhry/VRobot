using UnityEngine;
using System.Collections.Generic;
using Byn.Unity.Examples;
using TMPro;

public class NetMessageParser : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] [TextArea] private string defaultPosition;
    [SerializeField] private bool log;

    private ConferenceApp conferenceApp;
    private Dictionary<int, int> signals;
    private bool paused = true;

    private void Start()
    {
        signals = new Dictionary<int, int>();

        conferenceApp = FindObjectOfType<ConferenceApp>();

        ConferenceApp.OnMsgReceived += Parse;
        ConferenceApp.OnUserChanged += ResetRobot;
        ConferenceApp.OnUserDisconnected += ResetRobot;
    }

    private void OnDestroy()
    {
        ConferenceApp.OnMsgReceived -= Parse;
        ConferenceApp.OnUserChanged -= ResetRobot;
        ConferenceApp.OnUserDisconnected -= ResetRobot;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ResetRobot();
            paused = !paused;
        }

        if (statusText)
        {
            statusText.text = paused ? "Paused" : string.Empty;
        }
    }

    private void Parse(string msg)
    {
        if (paused)
        {
            return;
        }

        msg = msg[(msg.IndexOf(':') + 1)..];

        string[] signalStream = msg.Split(',');

        if (signalStream.Length <= 15)
        {
            return;
        }

        int id = 1;
        bool fingers = false;

        for (int i = 0; i < signalStream.Length - 2; i++)
        {
            string part = signalStream[i];
            if (!string.IsNullOrWhiteSpace(part))
            {
                if (int.TryParse(part, out int signal))
                {
                    bool send = !signals.ContainsKey(id);

                    if (!send)
                    {
                        send = Mathf.Abs(signal - signals[id]) >= 15f;
                    }

                    if (send)
                    {
                        string command = id + "," + signal;
                        MICRO micro = MICRO.TC_MOTORS;

                        if (id == 7 || id == 18)
                        {
                            micro = MICRO.M_MOTORS;
                        }
                        else
                        {
                            command = (fingers ? "c:" : "t:") + command;
                        }

                        if (SerialManager.Instance.SendSerialMessage(micro, command))
                        {
                            if (log)
                            {
                                Debug.Log(command);
                            }

                            if (!signals.TryAdd(id, signal))
                            {
                                signals[id] = signal;
                            }
                        }
                    }
                }
                else if (part == "f")
                {
                    fingers = true;
                    id = 20;
                }
                else
                {
                    return;
                }
            }

            id++;
        }

        if (float.TryParse(signalStream[^2], out float lSpeed) && float.TryParse(signalStream[^2], out float rSpeed) && lSpeed <= 5f && rSpeed <= 5f)
        {
            _ = SerialManager.Instance.SendSerialMessage(MICRO.XIAOMI_MOTORS, signalStream[^2] + "," + signalStream[^1]);
        }
    }

    private void ResetRobot()
    {
        if (conferenceApp.GetClientCount() != 0)
        {
            return;
        }

        Parse(defaultPosition);
    }
}
