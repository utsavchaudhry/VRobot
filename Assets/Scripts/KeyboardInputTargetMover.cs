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

    private enum State { Wheels, Left, Right }
    private State currentState;
    private Vector3 moveVector;
    private float forwardSpeed;

    private ConferenceApp conference;

    private void Start()
    {
        conference = FindObjectOfType<ConferenceApp>();
    }

    private void Update()
    {
        if (conference && !conference.IsActiveClient())
        {
            return;
        }

        if (Input.GetKeyDown(toggle))
        {
            if (currentState == State.Right)
            {
                currentState = 0;
            }
            else
            {
                currentState++;
            }
        }

        if (status)
        {
            status.text = string.Format("Control Mode:\n<color=green>{0}</color>\n<size=50%><i>Press {1} to Toggle", currentState.ToString(), toggle.ToString());
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
                leftIkTarget.Translate(handMoveSpeed * Time.deltaTime * moveVector);
                break;
            case State.Right:
                rightIkTarget.Translate(handMoveSpeed * Time.deltaTime * moveVector);
                break;
            default:
                break;
        }
    }
}
