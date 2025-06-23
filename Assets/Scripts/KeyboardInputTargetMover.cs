using UnityEngine;
using TMPro;
using Byn.Unity.Examples;

public class KeyboardInputTargetMover : MonoBehaviour
{
    public float LeftWheelSpeed { get; private set; }
    public float RightWheelSpeed { get; private set; }

    [SerializeField] private TextMeshProUGUI status;

    [Space]
    [Header("IK Target Transforms")]
    [Space]

    [SerializeField] private Transform leftIkTarget;
    [SerializeField] private Transform rightIkTarget;

    [Space]
    [Header("Settings")]
    [Space]

    [SerializeField] private KeyCode toggle = KeyCode.Space;
    [SerializeField] private float handMoveSpeed = 0.1f;
    [SerializeField] private float scrollSpeed = 50f;

    [Tooltip("Max linear speed for forward movement.")]
    [SerializeField] private float maxLinearSpeed = 1.0f;

    [Tooltip("Max turn speed (higher = faster turns).")]
    [SerializeField] private float maxTurnSpeed = 1.0f;

    [Tooltip("Percentage of forward speed to apply when moving backward (0-100).")]
    [Range(0f, 1f)] [SerializeField] private float backwardSpeedPercentage = .2f;

    [SerializeField] private bool phoneMode;
    [SerializeField] private bool useBounds;
    [SerializeField] private Vector3 minLeftHandPosition;
    [SerializeField] private Vector3 maxLeftHandPosition;
    [SerializeField] private Vector3 minRightHandPosition;
    [SerializeField] private Vector3 maxRightHandPosition;

    private enum State { Wheels, Left, Right }
    private State currentState;
    private Vector3 moveVector;
    private float forwardSpeed;

    private ConferenceApp conference;

    private void Start()
    {
        conference = FindObjectOfType<ConferenceApp>();
    }

    public void CycleControlMode()
    {
        if (conference && !conference.IsActiveClient())
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
    }

    private void Update()
    {
        if (conference && !conference.IsActiveClient())
        {
            return;
        }

        if (Input.GetKeyDown(toggle))
        {
            CycleControlMode();
        }

        if (status)
        {
            status.text = phoneMode ? string.Format("Control Mode:\n<color=green>{0}", currentState.ToString()) :
                string.Format("Control Mode:\n<color=green>{0}</color>\n<size=50%><i>Press {1} to Toggle", currentState.ToString(), toggle.ToString());
        }

        if (phoneMode)
        {
            moveVector.x = UltimateJoystick.GetHorizontalAxis("Movement");
            moveVector.y = UltimateJoystick.GetVerticalAxis("Movement");
            moveVector.z = 0f;
        }
        else
        {
            moveVector.x = Input.GetAxis("Horizontal");
            moveVector.y = Input.GetAxis("Vertical");
            moveVector.z = Input.GetAxis("Mouse ScrollWheel") * scrollSpeed;
        }

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
                leftIkTarget.Translate(handMoveSpeed * Time.deltaTime * moveVector);
                if (useBounds)
                {
                    leftIkTarget.transform.position = new Vector3(
                        Mathf.Clamp(leftIkTarget.transform.position.x, minLeftHandPosition.x, maxLeftHandPosition.x),
                        Mathf.Clamp(leftIkTarget.transform.position.y, minLeftHandPosition.y, maxLeftHandPosition.y),
                        Mathf.Clamp(leftIkTarget.transform.position.z, minLeftHandPosition.z, maxLeftHandPosition.z));
                }
                break;
            case State.Right:
                rightIkTarget.Translate(handMoveSpeed * Time.deltaTime * moveVector);
                if (useBounds)
                {
                    rightIkTarget.transform.position = new Vector3(
                        Mathf.Clamp(rightIkTarget.transform.position.x, minRightHandPosition.x, maxRightHandPosition.x),
                        Mathf.Clamp(rightIkTarget.transform.position.y, minRightHandPosition.y, maxRightHandPosition.y),
                        Mathf.Clamp(rightIkTarget.transform.position.z, minRightHandPosition.z, maxRightHandPosition.z));
                }
                break;
            default:
                break;
        }
    }
}
