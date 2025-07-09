using UnityEngine;

public class KeyboardInputTargetMover : TargetMover
{
    [SerializeField] private KeyCode toggle = KeyCode.T;
    [SerializeField] private float scrollSpeed = 15f;

    private enum State { Wheels, Left, Right }
    private State currentState;
    private Vector3 moveVector;
    private float forwardSpeed;

    protected override void Start()
    {
        base.Start();
    }

    protected override void UpdateStatusUI()
    {
        if (!status)
        {
            return;
        }

        status.text = string.Format("Control Mode:\n<color=green>{0}</color>\n<size=50%><i>Press {1} to Toggle", currentState.ToString(), toggle.ToString());
    }

    public void CycleControlMode()
    {
        if (IsMovementFrozen())
        {
            return;
        }

        if (currentState == State.Right)
        {
            currentState = 0;
        }
        else
        {
            currentState++;
        }

        UpdateStatusUI();
    }

    private void Update()
    {
        if (IsMovementFrozen())
        {
            return;
        }

        if (Input.GetKeyDown(toggle))
        {
            CycleControlMode();
        }

        moveVector.x = Input.GetAxis("Horizontal");
        moveVector.y = Input.GetAxis("Vertical");
        moveVector.z = Input.GetAxis("Mouse ScrollWheel") * scrollSpeed;

        switch (currentState)
        {
            case State.Wheels:

                if (moveVector.y < 0)
                {
                    // For backward motion, scale the speed by backwardSpeedPercentage
                    forwardSpeed = moveVector.y * maxLinearSpeed * backwardSpeedPercentage;
                }
                else
                {
                    // For forward motion, use the full maxLinearSpeed
                    forwardSpeed = moveVector.y * maxLinearSpeed;
                }

                // The turn speed remains unaffected
                float turnSpeed = moveVector.x * maxTurnSpeed;

                // Differential drive logic:
                //   Left  wheel = forwardSpeed + turnSpeed
                //   Right wheel = forwardSpeed - turnSpeed
                LeftWheelSpeed = forwardSpeed + turnSpeed;
                RightWheelSpeed = forwardSpeed - turnSpeed;

                break;
            case State.Left:
                TranslateAndClamp(leftIkTarget, moveVector, minLeftHandPosition, maxLeftHandPosition);
                break;
            case State.Right:
                TranslateAndClamp(rightIkTarget, moveVector, minRightHandPosition, maxRightHandPosition);
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
