using UnityEngine;

public class XRJoystickDifferentialDrive : MonoBehaviour
{
    [Tooltip("Max linear speed for forward movement.")]
    public float maxLinearSpeed = 1.0f;

    [Tooltip("Max turn speed (higher = faster turns).")]
    public float maxTurnSpeed = 1.0f;

    [Tooltip("Percentage of forward speed to apply when moving backward (0-100).")]
    [Range(0f, 1f)]
    public float backwardSpeedPercentage = .2f;

    public float LeftWheelSpeed { get; private set; }
    public float RightWheelSpeed { get; private set; }

    void Update()
    {
        float forwardSpeed;
        Vector2 thumbstick = InputManager.RightController.Joystick;
        if (thumbstick.y < 0)
        {
            // For backward motion, scale the speed by backwardSpeedPercentage
            forwardSpeed = thumbstick.y * maxLinearSpeed * backwardSpeedPercentage;
        }
        else
        {
            // For forward motion, use the full maxLinearSpeed
            forwardSpeed = thumbstick.y * maxLinearSpeed;
        }

        // The turn speed remains unaffected
        float turnSpeed = thumbstick.x * maxTurnSpeed;

        // Differential drive logic:
        //   Left  wheel = forwardSpeed + turnSpeed
        //   Right wheel = forwardSpeed - turnSpeed
        LeftWheelSpeed = forwardSpeed + turnSpeed;
        RightWheelSpeed = forwardSpeed - turnSpeed;
    }
}
