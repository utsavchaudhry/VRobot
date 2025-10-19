using UnityEngine;
using UnityEngine.UI;

public class JoystickInputTargetMoverPC : TargetMover
{
    [Space]

    [SerializeField] private GameObject lateralJoysticks;
    [SerializeField] private GameObject middleJoysticks;

    [Space]

    [SerializeField] private Button[] modeButtons;

    private enum State { Wheels, Camera, Arms }
    private State currentState;
    private float leftX, leftY, rightX, rightY, middleX, middleY;
    private float forwardSpeed;
    private bool buttonsConfigured;

    protected override void Start()
    {
        base.Start();
        FreezeCamera = currentState != State.Camera;
        ChangeControlMode(0);
    }

    private void ToggleJoystick()
    {
        if (currentState == State.Arms)
        {
            ToggleJoystick(lateralJoysticks, true);
        }
        else
        {
            ToggleJoystick(middleJoysticks, true);
        }
    }

    private void ToggleJoystick(GameObject joystick, bool state)
    {
        if (!joystick)
        {
            return;
        }

        joystick.SetActive(state);

        if (joystick == lateralJoysticks)
        {
            if (middleJoysticks)
            {
                middleJoysticks.SetActive(!state);
            }
        }
        else if (lateralJoysticks)
        {
            lateralJoysticks.SetActive(!state);
        }
    }

    protected override void UpdateStatusUI()
    {
        for (int i = 0; i < modeButtons.Length; i++)
        {
            modeButtons[i].interactable = i != (int)currentState;
            if (!buttonsConfigured)
            {
                int index = i;
                modeButtons[i].onClick.AddListener(() => ChangeControlMode(index));
            }
        }

        buttonsConfigured = true;

        if (!status)
        {
            return;
        }

        status.text = status.text = string.Format("Control Mode:\n<color=green>{0}", currentState.ToString());
    }

    private void ChangeControlMode(int mode)
    {
        if (IsMovementFrozen() || (int)currentState == mode)
        {
            //return;
        }

        currentState = (State)mode;

        FreezeCamera = currentState == State.Arms;

        UpdateStatusUI();

        ToggleJoystick();
    }

    private void Update()
    {
        if (IsMovementFrozen())
        {
            //return;
        }

        leftX = UltimateJoystick.GetHorizontalAxis("Left");
        leftY = UltimateJoystick.GetVerticalAxis("Left");
        rightX = UltimateJoystick.GetHorizontalAxis("Right");
        rightY = UltimateJoystick.GetVerticalAxis("Right");
        middleX = UltimateJoystick.GetHorizontalAxis("Middle");
        middleY = UltimateJoystick.GetVerticalAxis("Middle");

        switch (currentState)
        {
            case State.Wheels:
                if (middleY < 0)
                {
                    // For backward motion, scale the speed by backwardSpeedPercentage
                    forwardSpeed = middleY * maxLinearSpeed * backwardSpeedPercentage;
                }
                else
                {
                    // For forward motion, use the full maxLinearSpeed
                    forwardSpeed = middleY * maxLinearSpeed;
                }

                // The turn speed remains unaffected
                float turnSpeed = middleX * maxTurnSpeed;

                // Differential drive logic:
                //   Left  wheel = forwardSpeed + turnSpeed
                //   Right wheel = forwardSpeed - turnSpeed
                LeftWheelSpeed = forwardSpeed + turnSpeed;
                RightWheelSpeed = forwardSpeed - turnSpeed;

                break;

            case State.Arms:

                TranslateAndClamp(leftIkTarget, new Vector3(leftX, leftY, 0f), minLeftHandPosition, maxLeftHandPosition);
                TranslateAndClamp(rightIkTarget, new Vector3(rightX, rightY, 0f), minRightHandPosition, maxRightHandPosition);

                break;
            case State.Camera:
                break;
            default:
                break;
        }

        if (currentState != State.Wheels)
        {
            LeftWheelSpeed = RightWheelSpeed = 0f;
        }
    }
}
