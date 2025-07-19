using UnityEngine;
using TMPro;
using Byn.Unity.Examples;

public abstract class TargetMover : MonoBehaviour
{
    public static bool FreezeCamera { get; protected set; }
    public float LeftWheelSpeed { get; protected set; }
    public float RightWheelSpeed { get; protected set; }

    [SerializeField] protected TextMeshProUGUI status;

    [Space]
    [Header("IK Target Transforms")]
    [Space]

    [SerializeField] protected Transform leftIkTarget;
    [SerializeField] protected Transform rightIkTarget;

    [Space]
    [Header("Settings")]
    [Space]

    [SerializeField] protected float handMoveSpeed = 0.1f;

    [Tooltip("Max linear speed for forward movement.")]
    [SerializeField] protected float maxLinearSpeed = 2.5f;

    [Tooltip("Max turn speed (higher = faster turns).")]
    [SerializeField] protected float maxTurnSpeed = 1.75f;

    [Tooltip("Percentage of forward speed to apply when moving backward (0-100).")]
    [Range(0f, 1f)] [SerializeField] protected float backwardSpeedPercentage = 0.5f;

    [SerializeField] protected Vector3 minLeftHandPosition = new(-0.7f, -0.6f, 0.1f);
    [SerializeField] protected Vector3 maxLeftHandPosition = new(0.15f, 0.2f, 0.5f);
    [SerializeField] protected Vector3 minRightHandPosition = new(-0.15f, -0.6f, 0.1f);
    [SerializeField] protected Vector3 maxRightHandPosition = new(0.7f, 0.2f, 0.5f);

    private ConferenceApp conference;

    protected virtual void Start()
    {
        conference = FindObjectOfType<ConferenceApp>();
        FreezeCamera = false;
        UpdateStatusUI();
    }

    protected bool IsMovementFrozen()
    {
        return conference && !conference.IsActiveClient();
    }

    protected abstract void UpdateStatusUI();

    protected void TranslateAndClamp(Transform t, Vector3 translateVector, Vector3 minPosition, Vector3 maxPosition)
    {
        if (translateVector.sqrMagnitude == 0f)
        {
            return;
        }

        t.Translate(handMoveSpeed * Time.deltaTime * translateVector);
        t.transform.position = new Vector3(
            Mathf.Clamp(t.transform.position.x, minPosition.x, maxPosition.x),
            Mathf.Clamp(t.transform.position.y, minPosition.y, maxPosition.y),
            Mathf.Clamp(t.transform.position.z, minPosition.z, maxPosition.z));
    }
}
