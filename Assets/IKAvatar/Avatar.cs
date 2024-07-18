using UnityEngine;

//Class that moves the target of Avatar's IK corresponding to the position of the VR user

[System.Serializable]
public class VRMap
{
    [SerializeField] private Transform vrTarget;
    [SerializeField] private Transform rigTarget;
    [SerializeField] private Vector3 trackingPositionOffset;
    [SerializeField] private Vector3 trackingRotationOffset;

    public void Map()
    {
        rigTarget.SetPositionAndRotation(vrTarget.TransformPoint(trackingPositionOffset), vrTarget.rotation * Quaternion.Euler(trackingRotationOffset));
    }
}

public class Avatar : MonoBehaviour
{
    public VRMap head;
    public VRMap leftHand;
    public VRMap rightHand;

    public Transform headConstraint;
    Vector3 headBodyOffset;
    private float turnSmoothness = 0.8f;

    void Start()
    {
        headBodyOffset = transform.position - headConstraint.position;
    }

    void Update()
    {
        transform.position = headConstraint.position + headBodyOffset;
        transform.forward = Vector3.Lerp(transform.forward, Vector3.ProjectOnPlane(headConstraint.up, Vector3.up).normalized, Time.deltaTime * turnSmoothness);

        head.Map();
        leftHand.Map();
        rightHand.Map();
    }
}
