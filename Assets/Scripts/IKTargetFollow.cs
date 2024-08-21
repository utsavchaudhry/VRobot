using UnityEngine;

public class IKTargetFollow : MonoBehaviour
{
    [SerializeField] private Transform controllerTransform;
    [SerializeField] private float deadZone;

    private Transform vrobotTransform;
    private bool isRightTarget;

    private void Start()
    {
        VRobot vrobot = FindObjectOfType<VRobot>();
        if (vrobot)
        {
            vrobotTransform = vrobot.transform;
        }

        isRightTarget = controllerTransform.name.ToLower().Contains("right");
    }

    private void Update()
    {
        if (controllerTransform && InSafeZone() &&
            (isRightTarget ? InputManager.RightController.IsValid : InputManager.LeftController.IsValid))
        {
            transform.SetPositionAndRotation(controllerTransform.position, controllerTransform.rotation);
        }
    }

    private bool InSafeZone()
    {
        return vrobotTransform
            ? Mathf.Abs(controllerTransform.position.x - vrobotTransform.position.x) > deadZone ||
                Mathf.Abs(controllerTransform.position.z - vrobotTransform.position.z) > deadZone
            : true;
    }
}
