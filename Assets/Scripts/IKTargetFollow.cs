using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

public class IKTargetFollow : MonoBehaviour
{
    [SerializeField] private Transform controllerTransform;

    private InputDevice controller;
    private Vector3 defaultPosition;
    private Quaternion defaultRotation;

    private void Start()
    {
        defaultPosition = transform.position;
        defaultRotation = transform.rotation;
    }

    private void Update()
    {
        if (!controllerTransform)
        {
            return;
        }

        if (controller.isValid)
        {
            transform.SetPositionAndRotation(controllerTransform.position, controllerTransform.rotation);
        }
        else
        {
            transform.SetPositionAndRotation(defaultPosition, defaultRotation);
            SetupController();
        }
    }

    private void SetupController()
    {
        if (!controllerTransform)
        {
            return;
        }

        List<InputDevice> devices = new();

        if (controllerTransform.name.ToLower().Contains("left"))
        {
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, devices);
        }
        else
        {
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, devices);
        }

        if (devices.Count > 0)
        {
            controller = devices[0];
        }
    }
}
