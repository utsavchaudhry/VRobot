using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

public class IKTargetFollow : MonoBehaviour
{
    private enum Side { Left, Right }

    [SerializeField] private Side side;

    private InputDevice controller;

    private void Update()
    {
        if (!controller.isValid)
        {
            SetupController();
            return;
        }

        if (controller.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 controllerPosition))
        {
            transform.position = controllerPosition;
        }

        if (controller.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion controllerRotation))
        {
            transform.rotation = controllerRotation;
        }
    }

    private void SetupController()
    {
        List<InputDevice> devices = new();

        switch (side)
        {
            case Side.Left:
                InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, devices);
                break;
            case Side.Right:
                InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, devices);
                break;
            default:
                break;
        }

        if (devices.Count > 0)
        {
            controller = devices[0];
        }
    }
}
