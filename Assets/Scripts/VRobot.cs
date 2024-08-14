using UnityEngine;

public class VRobot : MonoBehaviour
{
    [SerializeField] private Transform head;
    [SerializeField] private Vector3 offset = new(0f, -0.15f, -0.15f);

    [SerializeField] private Transform lTarget;
    [SerializeField] private Transform rTarget;

    private static Quaternion rot;

    private void Start()
    {
        rot = transform.rotation;
        InputManager.OnSecondaryButtonDown += Calibrate;
    }

    private void OnDestroy()
    {
        InputManager.OnSecondaryButtonDown += Calibrate;
    }

    private void Update()
    {
        if (head)
        {
            transform.SetPositionAndRotation(head.position + (transform.right * offset.x) + (transform.up * offset.y) + (transform.forward * offset.z)
                , rot);
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            Calibrate();
        }
    }
    
    public static void ResetYaw(float yaw)
    {
        rot = Quaternion.Euler(Vector3.up * yaw);
    }

    private void Calibrate()
    {
        float avgArmSpan = (Vector3.Distance(rTarget.position, transform.position + (transform.right * 0.1f)) +
            Vector3.Distance(lTarget.position, transform.position + (transform.right * -0.1f))) / 2f;
        transform.localScale = Vector3.one * avgArmSpan / 0.4f;
    }
}
