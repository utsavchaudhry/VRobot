using UnityEngine;
using System.Collections.Generic;
using Byn.Unity.Examples;
using TMPro;

public class NetMessageParser : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] [TextArea] private string defaultPosition;
    [SerializeField] private bool log;

    private Dictionary<int, int> signals;
    public static bool paused;

    private void Start()
    {
        signals = new Dictionary<int, int>();

        ConferenceApp.OnMsgReceived += Parse;
        ConferenceApp.OnUserChanged += StopWheels;
        ConferenceApp.OnUserDisconnected += StopWheels;

        paused = false;
    }

    private void OnDestroy()
    {
        ConferenceApp.OnMsgReceived -= Parse;
        ConferenceApp.OnUserChanged -= StopWheels;
        ConferenceApp.OnUserDisconnected -= StopWheels;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            paused = !paused;
            if (statusText)
            {
                statusText.text = paused ? "Paused" : string.Empty;
            }
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            Parse(defaultPosition);
        }
    }

    private void Parse(string msg)
    {
        if (string.IsNullOrWhiteSpace(msg) || !msg.Contains(',') || paused)
        {
            return;
        }

        msg = msg[(msg.IndexOf(':') + 1)..];

        int id = 1;

        string[] signalStream = msg.Split(',');
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

        _ = SerialManager.Instance.SendSerialMessage(MICRO.XIAOMI_MOTORS, signalStream[^2] + "," + signalStream[^1]);
    }

    private void StopWheels()
    {
        _ = SerialManager.Instance.SendSerialMessage(MICRO.XIAOMI_MOTORS, "0,0");
    }
}
