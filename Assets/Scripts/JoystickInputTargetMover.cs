using UnityEngine;

public class JoystickInputTargetMover : TargetMover
{
    private enum State { Wheels, Arms }
    private State currentState;
    private float leftX, leftY, rightX, rightY;
    private float forwardSpeed;

    protected override void Start()
    {
        base.Start();
        FreezeCamera = currentState == State.Arms;
    }

    protected override void UpdateStatusUI()
    {
        if (!status)
        {
            return;
        }

        status.text = status.text = string.Format("Control Mode:\n<color=green>{0}", currentState.ToString());
    }

    public void CycleControlMode()
    {
        if (IsMovementFrozen())
        {
            return;
        }

        if (currentState == State.Arms)
        {
            currentState = 0;
        }
        else
        {
            currentState++;
        }

        FreezeCamera = currentState == State.Arms;

        UpdateStatusUI();
    }

    private void Update()
    {
        if (IsMovementFrozen())
        {
            return;
        }

        leftX = UltimateJoystick.GetHorizontalAxis("Left");
        leftY = UltimateJoystick.GetVerticalAxis("Left");
        rightX = UltimateJoystick.GetHorizontalAxis("Right");
        rightY = UltimateJoystick.GetVerticalAxis("Right");

        switch (currentState)
        {
            case State.Wheels:
                if (leftY < 0)
                {
                    // For backward motion, scale the speed by backwardSpeedPercentage
                    forwardSpeed = leftY * maxLinearSpeed * backwardSpeedPercentage;
                }
                else
                {
                    // For forward motion, use the full maxLinearSpeed
                    forwardSpeed = leftY * maxLinearSpeed;
                }

                // The turn speed remains unaffected
                float turnSpeed = leftX * maxTurnSpeed;

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

            default:
                break;
        }

        if (currentState != State.Wheels)
        {
            LeftWheelSpeed = RightWheelSpeed = 0f;
        }
    }
}
