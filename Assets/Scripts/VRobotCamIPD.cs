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
    private bool valid;

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
        Set();

        InputManager.RightController.PrimaryBtn.OnDown += ValidDown;
        InputManager.RightController.PrimaryBtn.OnUp += ValidUp;
    }

    private void OnDestroy()
    {
        if (InputManager.RightController == null)
        {
            return;
        }

        InputManager.RightController.PrimaryBtn.OnDown -= ValidDown;
        InputManager.RightController.PrimaryBtn.OnUp -= ValidUp;
    }

    private void ValidDown()
    {
        valid = true;
    }

    private void ValidUp()
    {
        valid = false;
    }

    private void Update()
    {
        float ipdDelta = valid ? InputManager.LeftController.Joystick.x * sensitivity * Time.deltaTime : 0f;

        if (ipdDelta != 0f)
        {
            Set(ipdDelta);
        }
    }

    private void Set(float ipdDelta = 0f)
    {
        if (ipdDelta != 0f)
        {
            ipdOffset = Mathf.Clamp(ipdOffset + ipdDelta, -range, range);
            PlayerPrefs.SetFloat(ipdSaveKey, ipdOffset);
        }

        if (left)
        {
            left.transform.position = (Vector3.right * ipdOffset) + originalPositionLeft;
        }

        if (right)
        {
            right.transform.position = (Vector3.left * ipdOffset) + originalPositionRight;
        }
    }
}
