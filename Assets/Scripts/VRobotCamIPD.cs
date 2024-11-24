using UnityEngine;

public class VRobotCamIPD : MonoBehaviour
{
    [SerializeField] private Transform left;
    [SerializeField] private Transform right;
    [SerializeField] [Range(0f, 250f)] private float range = 200f;
    [SerializeField] private float sensitivity = 10f;

    private Vector3 originalPositionLeft;
    private Vector3 originalPositionRight;
    private float ipdOffset;
    private string ipdSaveKey = "IPD";
    private bool setup;

    private Vector3 GetPosition(Transform t)
    {
        if (!t)
        {
            return Vector3.zero;
        }

        return t.position;
    }

    private void Start()
    {
        originalPositionLeft = GetPosition(left);
        originalPositionRight = GetPosition(right);
        ipdOffset = PlayerPrefs.GetFloat(ipdSaveKey);
    }

    private void Update()
    {
        if (range == 0f)
        {
            return;
        }

        float ipdDelta = InputManager.LeftController.Joystick.x * sensitivity * Time.deltaTime;

        if (ipdDelta != 0f || !setup)
        {
            ipdOffset = Mathf.Clamp(ipdOffset + ipdDelta, -range, range);

            if (left)
            {
                left.transform.position = (Vector3.right * ipdOffset) + originalPositionLeft;
            }

            if (right)
            {
                right.transform.position = (Vector3.left * ipdOffset) + originalPositionRight;
            }

            PlayerPrefs.SetFloat(ipdSaveKey, ipdOffset);

            setup = true;
        }
    }
}
