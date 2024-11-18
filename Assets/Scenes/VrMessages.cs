using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;
using Byn.Unity.Examples;
public class VrMessages : MonoBehaviour
{
    private InputDevice leftController;
    private InputDevice rightController;
    public string deviceMessage = "";
    private ChatApp _chatAppobj;

    private void Awake()
    {
        _chatAppobj = FindObjectOfType<ChatApp>();
    }
    void Start()
    {
        List<InputDevice> devices = new List<InputDevice>();

        // Get Left Controller
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, devices);
        if (devices.Count > 0) leftController = devices[0];

        // Get Right Controller
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, devices);
        if (devices.Count > 0) rightController = devices[0];
    }

    // Update is called once per frame
    void Update()
    {

        if (leftController.isValid)
        {
            bool primaryButtonValue;
            if (leftController.TryGetFeatureValue(CommonUsages.primaryButton, out primaryButtonValue) && primaryButtonValue)
            {
                Debug.Log("Left Primary Button Pressed");
                deviceMessage = "Left Primary Button Pressed";
                SendMsg();
            }

            bool triggerPressed;
            if (leftController.TryGetFeatureValue(CommonUsages.triggerButton, out triggerPressed) && triggerPressed)
            {
                Debug.Log("Left Trigger Button Pressed");
                deviceMessage = "Left Trigger Button Pressed";
                SendMsg();
            }
        }

        if (rightController.isValid)
        {
            bool primaryButtonValue;
            if (rightController.TryGetFeatureValue(CommonUsages.primaryButton, out primaryButtonValue) && primaryButtonValue)
            {
                Debug.Log("Right Primary Button Pressed");
                deviceMessage = "Right Primary Button Pressed";
                SendMsg();
            }

            bool triggerPressed;
            if (rightController.TryGetFeatureValue(CommonUsages.triggerButton, out triggerPressed) && triggerPressed)
            {
                Debug.Log("Right Trigger Button Pressed");
                deviceMessage = "Right Trigger Button Pressed";
                SendMsg();
            }
        }
       
    }

    void SendMsg()
    {
        if (_chatAppobj.isServerReady)
        {
            _chatAppobj.SendButtonPressed(deviceMessage);
        }
    }
}
