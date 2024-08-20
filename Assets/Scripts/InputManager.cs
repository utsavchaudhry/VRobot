using UnityEngine;
using System;
using UnityEngine.XR;
using System.Collections.Generic;

public class InputManager : MonoBehaviour
{
    public class ControllerButton
    {
        public event Action OnDown;
        public event Action OnUp;

        private bool previousControllerState;
        private bool previousKeyboardState;

        public void Update(InputDevice controller, InputFeatureUsage<bool> btn, KeyCode key)
        {
            if (controller.isValid && controller.TryGetFeatureValue(btn, out bool currentState) &&
                CheckStateChange(currentState, ref previousControllerState))
            {
                return;
            }

            _ = CheckStateChange(Input.GetKey(key), ref previousKeyboardState);
        }

        private bool CheckStateChange(bool currentState, ref bool previousState)
        {
            if (currentState == previousState)
            {
                return false;
            }

            if (currentState)
            {
                OnDown?.Invoke();
            }
            else
            {
                OnUp?.Invoke();
            }

            previousState = currentState;

            return true;
        }
    }

    public class MyController
    {
        public ControllerButton PrimaryBtn { get; private set; }
        public ControllerButton SecondaryBtn { get; private set; }
        public Vector2 Joystick { get; private set; }
        public float Trigger { get; private set; }
        public bool IsValid { get; private set; }

        private InputDevice controller;
        private bool isRightController;

        public MyController(bool _isRightController)
        {
            PrimaryBtn = new ControllerButton();
            SecondaryBtn = new ControllerButton();

            isRightController = _isRightController;
        }

        private bool SetDevice()
        {
            if (controller.isValid && controller.characteristics == InputDeviceCharacteristics.Controller &&
                    controller.characteristics == (isRightController ? InputDeviceCharacteristics.Right : InputDeviceCharacteristics.Left))
            {
                return true;
            }
            else
            {
                List<InputDevice> devices = new();

                if (isRightController)
                {
                    InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, devices);
                }
                else
                {
                    InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, devices);
                }

                if (devices.Count > 0)
                {
                    controller = devices[0];
                }
            }

            return controller.isValid;
        }

        public void Update()
        {
            if (SetDevice())
            {
                IsValid = true;

                PrimaryBtn.Update(controller, CommonUsages.primaryButton, isRightController ? KeyCode.L : KeyCode.R);
                SecondaryBtn.Update(controller, CommonUsages.secondaryButton, isRightController ? KeyCode.P : KeyCode.C);

                if (controller.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 joystick))
                {
                    Joystick = joystick;
                }

                if (controller.TryGetFeatureValue(CommonUsages.trigger, out float trigger))
                {
                    Trigger = trigger;
                }
            }
            else
            {
                IsValid = false;
            }
        }
    }

    public static MyController RightController { get; private set; }
    public static MyController LeftController { get; private set; }

    private void Awake()
    {
        RightController = new MyController(true);
        LeftController = new MyController(false);
    }

    private void Update()
    {
        RightController.Update();
        LeftController.Update();
    }
}
