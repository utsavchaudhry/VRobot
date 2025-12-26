using UnityEngine;
using Byn.Unity.Examples;

public class MouseHead : MonoBehaviour
{
    [Header("Mouse Sensitivity")]
    [SerializeField] private float sensitivityX = 2f; // Yaw sensitivity.
    [SerializeField] private float sensitivityY = 2f; // Pitch sensitivity.

    [Header("Yaw Limit (Symmetric)")]
    [SerializeField] private float yawLimit = 60f; // Allowed yaw: [-yawLimit, yawLimit].

    [Header("Pitch Limits (Separate)")]
    [SerializeField] private float maxUpPitch = 45f;  // Maximum upward tilt (look up).
    [SerializeField] private float maxDownPitch = 60f; // Maximum downward tilt (look down).

    private ConferenceApp conference;

    // Accumulated rotation values.
    private float yaw = 0f;   // Horizontal rotation (around Y-axis).
    private float pitch = 0f; // Vertical rotation (around X-axis).
    private float mouseX, mouseY;
    private enum InputState { JoystickPhone, JoystickPC, MousePC }
    private InputState input;

    void Start()
    {
        conference = FindObjectOfType<ConferenceApp>();
        input = FindObjectOfType<JoystickInputTargetMoverPC>()
            ? InputState.JoystickPC
            : FindObjectOfType<KeyboardInputTargetMover>() ? InputState.MousePC : InputState.JoystickPhone;
    }

    void Update()
    {
        if (TargetMover.FreezeCamera || (conference && !conference.IsActiveClient()))
        {
            return;
        }

        switch (input)
        {
            case InputState.JoystickPhone:
                mouseX = UltimateJoystick.GetHorizontalAxis("Right") * sensitivityX;
                mouseY = UltimateJoystick.GetVerticalAxis("Right") * sensitivityY;
                break;
            case InputState.JoystickPC:
                mouseX = UltimateJoystick.GetHorizontalAxis("Middle") * sensitivityX;
                mouseY = UltimateJoystick.GetVerticalAxis("Middle") * sensitivityY;
                break;
            case InputState.MousePC:
                mouseX = Input.GetAxis("Mouse X");
                mouseY = Input.GetAxis("Mouse Y");
                break;
            default:
                break;
        }

        // Update yaw (horizontal rotation). 
        yaw += mouseX;
        yaw = Mathf.Clamp(yaw, -yawLimit, yawLimit);

        // Update pitch (vertical rotation).
        // Invert mouseY to make upward movement result in negative pitch.
        pitch -= mouseY;
        // Clamp pitch: upward rotation is limited to -maxUpPitch, downward to +maxDownPitch.
        pitch = Mathf.Clamp(pitch, -maxUpPitch, maxDownPitch);

        // Apply the rotation without roll.
        transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}
