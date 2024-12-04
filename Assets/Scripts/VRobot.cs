using UnityEngine;
using TMPro;

public class VRobot : MonoBehaviour
{
    public static bool IsPaused { get; private set; }

    [SerializeField] private Transform head;
    [SerializeField] private Vector3 offset = new(0f, -0.15f, -0.15f);

    [Space]

    [SerializeField] private Transform lTarget;
    [SerializeField] private Transform rTarget;

    [Space]

    [SerializeField] private TextMeshProUGUI[] statusTexts;

    [Space]

    [SerializeField] private bool allowReset;

    private static Quaternion rot;

    private void Start()
    {
        IsPaused = true;

        rot = transform.rotation;

        InputManager.LeftController.SecondaryBtn.OnDown += Calibrate;
        InputManager.LeftController.PrimaryBtn.OnDown += ResetYaw;
        InputManager.RightController.SecondaryBtn.OnDown += TogglePauseState;

        if (PlayerPrefs.HasKey("VRobotSize"))
        {
            transform.localScale = Vector3.one * PlayerPrefs.GetFloat("VRobotSize");
        }
    }

    private void OnDestroy()
    {
        if (InputManager.LeftController != null)
        {
            InputManager.LeftController.SecondaryBtn.OnDown -= Calibrate;
            InputManager.LeftController.PrimaryBtn.OnDown -= ResetYaw;
        }

        if (InputManager.RightController != null)
        {
            InputManager.RightController.SecondaryBtn.OnDown -= TogglePauseState;
        }
    }

    private void Update()
    {
        if (head)
        {
            transform.SetPositionAndRotation(
                head.position + (transform.right * offset.x) + (transform.up * offset.y) + (transform.forward * offset.z),
                rot);
        }

        foreach (TextMeshProUGUI item in statusTexts)
        {
            item.text = IsPaused ? "Paused" : string.Empty;
        }
    }

    private void TogglePauseState()
    {
        IsPaused = !IsPaused;
    }
    
    private void ResetYaw()
    {
        if (!allowReset)
        {
            return;
        }

        rot = Quaternion.Euler(Vector3.up * head.eulerAngles.y);
    }

    private void Calibrate()
    {
        float avgArmSpan = (Vector3.Distance(rTarget.position, transform.position + (transform.right * 0.1f)) +
            Vector3.Distance(lTarget.position, transform.position + (transform.right * -0.1f))) / 2f;
        transform.localScale = Vector3.one * avgArmSpan / 0.5f;

        PlayerPrefs.SetFloat("VRobotSize", transform.localScale.x);

        Debug.Log("Calibrated factor: " + transform.localScale.x);
    }
}
