using UnityEngine;
using System;
using UnityEngine.XR;
using System.Collections.Generic;

public class InputManager : MonoBehaviour
{
    public enum JoystickDirection { Idle, Right, Left, Up, Down }

    // Input Actions
    public static event Action OnTriggerDown;
    public static event Action OnTriggerUp;
    public static event Action OnGripDown;
    public static event Action OnGripUp;
    public static event Action OnPrimaryButtonDown;
    public static event Action OnPrimaryButtonUp;
    public static event Action OnSecondaryButtonDown;
    public static event Action OnSecondaryButtonUp;
    public static event Action<JoystickDirection> OnJoystickStateChanged;

    private static InputDevice rightController;
    private static InputDevice leftController;
    private JoystickDirection joystickDirection = JoystickDirection.Idle;
    private JoystickDirection leftjoystickDirection = JoystickDirection.Idle;
    private JoystickDirection rightjoystickDirection = JoystickDirection.Idle;
    private bool trigger;
    private bool lefttrigger;
    private bool righttrigger;
    private bool grip;
    private bool leftgrip;
    private bool rightgrip;
    private bool primaryButton;
    private bool leftprimaryButton;
    private bool rightprimaryButton;
    private bool secondaryButton;
    private bool leftsecondaryButton;
    private bool rightsecondaryButton;

    private void Start()
    {
        SetDevice();
    }

    //sets xr devices
    private void SetDevice()
    {
        if (rightController.isValid && leftController.isValid)
        {
            return;
        }

        List<InputDevice> devices = new();

        if (!rightController.isValid)
        {
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, devices);
            if (devices.Count > 0)
            {
                rightController = devices[0];
            }
        }

        if (!leftController.isValid)
        {
            devices.Clear();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, devices);
            if (devices.Count > 0)
            {
                leftController = devices[0];
            }
        }
    }

    //compares current and previous state and fires event if the state is changed
    private bool CheckState(bool previousLeft, bool currentLeft, bool previousRight, bool currentRight, bool previous, Action down, Action up)
    {
        int a = 0;

        if (currentLeft != previousLeft && currentRight == previousRight && previousRight == false && currentLeft != previous)
        {
            a = currentLeft ? -1 : 1;
        }
        else if (currentRight != previousRight && currentLeft == previousLeft && previousLeft == false && currentRight != previous)
        {
            a = currentRight ? -1 : 1;
        }

        if (a < 0)
        {
            down?.Invoke();
        }
        else if (a > 0)
        {
            up?.Invoke();
        }

        return a != 0 ? !previous : previous;
    }

    private void Update()
    {
        SetDevice();

        bool _leftTrigger = false;
        bool _rightTrigger = false;
        bool _leftGrip = false;
        bool _rightGrip = false;
        bool _leftPrimaryButton = false;
        bool _rightPrimaryButton = false;
        bool _leftSecondaryButton = false;
        bool _rightSecondaryButton = false;
        Vector2 _leftJoystickValue = Vector2.zero;
        Vector2 _rightJoystickValue = Vector2.zero;
        JoystickDirection _leftJoystickDirection = JoystickDirection.Idle;
        JoystickDirection _rightJoystickDirection = JoystickDirection.Idle;

        if (leftController.isValid)
        {
            leftController.TryGetFeatureValue(CommonUsages.triggerButton, out _leftTrigger);
            leftController.TryGetFeatureValue(CommonUsages.gripButton, out _leftGrip);
            leftController.TryGetFeatureValue(CommonUsages.primaryButton, out _leftPrimaryButton);
            leftController.TryGetFeatureValue(CommonUsages.secondaryButton, out _leftSecondaryButton);
            leftController.TryGetFeatureValue(CommonUsages.primary2DAxis, out _leftJoystickValue);
        }

        if (rightController.isValid)
        {
            rightController.TryGetFeatureValue(CommonUsages.triggerButton, out _rightTrigger);
            rightController.TryGetFeatureValue(CommonUsages.gripButton, out _rightGrip);
            rightController.TryGetFeatureValue(CommonUsages.primaryButton, out _rightPrimaryButton);
            rightController.TryGetFeatureValue(CommonUsages.secondaryButton, out _rightSecondaryButton);
            rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out _rightJoystickValue);
        }

        trigger = CheckState(lefttrigger, _leftTrigger, righttrigger, _rightTrigger, trigger, OnTriggerDown, OnTriggerUp);
        grip = CheckState(leftgrip, _leftGrip, rightgrip, _rightGrip, grip, OnGripDown, OnGripUp);
        primaryButton = CheckState(leftprimaryButton, _leftPrimaryButton, rightprimaryButton, _rightPrimaryButton, primaryButton, OnPrimaryButtonDown, OnPrimaryButtonUp);
        secondaryButton = CheckState(leftsecondaryButton, _leftSecondaryButton, rightsecondaryButton, _rightSecondaryButton, secondaryButton, OnSecondaryButtonDown, OnSecondaryButtonUp);

        if (Mathf.Abs(_leftJoystickValue.x) > 0.5f || Mathf.Abs(_leftJoystickValue.y) > 0.5f)
        {
            if (Mathf.Abs(_leftJoystickValue.x) > Mathf.Abs(_leftJoystickValue.y))
            {
                if (_leftJoystickValue.x > 0)
                {
                    _leftJoystickDirection = JoystickDirection.Right;
                }
                else
                {
                    _leftJoystickDirection = JoystickDirection.Left;
                }
            }
            else
            {
                if (_leftJoystickValue.y > 0)
                {
                    _leftJoystickDirection = JoystickDirection.Up;
                }
                else
                {
                    _leftJoystickDirection = JoystickDirection.Down;
                }
            }
        }

        if ((Mathf.Abs(_rightJoystickValue.x) > 0.5f || Mathf.Abs(_rightJoystickValue.y) > 0.5f))
        {
            if (Mathf.Abs(_rightJoystickValue.x) > Mathf.Abs(_rightJoystickValue.y))
            {
                if (_rightJoystickValue.x > 0)
                {
                    _rightJoystickDirection = JoystickDirection.Right;
                }
                else
                {
                    _rightJoystickDirection = JoystickDirection.Left;
                }
            }
            else
            {
                if (_rightJoystickValue.y > 0)
                {
                    _rightJoystickDirection = JoystickDirection.Up;
                }
                else
                {
                    _rightJoystickDirection = JoystickDirection.Down;
                }
            }
        }

        if (_leftJoystickDirection != leftjoystickDirection && _rightJoystickDirection == rightjoystickDirection && rightjoystickDirection == JoystickDirection.Idle && _leftJoystickDirection != joystickDirection)
        {
            OnJoystickStateChanged?.Invoke(_leftJoystickDirection);
            joystickDirection = _leftJoystickDirection;
        }
        else if (_rightJoystickDirection != rightjoystickDirection && _leftJoystickDirection == leftjoystickDirection && leftjoystickDirection == JoystickDirection.Idle && _rightJoystickDirection != joystickDirection)
        {
            OnJoystickStateChanged?.Invoke(_rightJoystickDirection);
            joystickDirection = _rightJoystickDirection;
        }

        lefttrigger = _leftTrigger;
        leftgrip = _leftGrip;
        leftprimaryButton = _leftPrimaryButton;
        leftsecondaryButton = _leftSecondaryButton;
        leftjoystickDirection = _leftJoystickDirection;

        righttrigger = _rightTrigger;
        rightgrip = _rightGrip;
        rightprimaryButton = _rightPrimaryButton;
        rightsecondaryButton = _rightSecondaryButton;
        rightjoystickDirection = _rightJoystickDirection;
    }
}
