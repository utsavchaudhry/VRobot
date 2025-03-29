using UnityEngine;

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

    // Accumulated rotation values.
    private float yaw = 0f;   // Horizontal rotation (around Y-axis).
    private float pitch = 0f; // Vertical rotation (around X-axis).

    void Start()
    {
        // Lock the cursor to the center of the screen.
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Get mouse input.
        float mouseX = Input.GetAxis("Mouse X") * sensitivityX;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivityY;

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
