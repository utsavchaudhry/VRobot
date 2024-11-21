using UnityEngine;

public class VRobotCamIPD : MonoBehaviour
{
    [SerializeField] private Transform left;
    [SerializeField] private Transform right;
    [SerializeField] [Range(0f, 250f)] private float range = 200f;
    [SerializeField] private float sensitivity = 10f;
    [SerializeField] private bool testWithKeyboard;

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

#if !UNITY_EDITOR

        testWithKeyboard = false;

#endif
    }

    private void SetPosition(Transform t, Vector3 position)
    {
        if (!t)
        {
            return;
        }

        t.position = position;
    }

    private void Update()
    {
        if (range == 0f)
        {
            return;
        }

        float ipdDelta = (testWithKeyboard ? Input.GetAxis("Horizontal") : InputManager.LeftController.Joystick.x) * sensitivity * Time.deltaTime;

        if (ipdDelta != 0f || !setup)
        {
            ipdOffset = Mathf.Clamp(ipdOffset + ipdDelta, -range, range);
            SetPosition(left, (Vector3.right * ipdOffset) + originalPositionLeft);
            SetPosition(right, (Vector3.left * ipdOffset) + originalPositionRight);
            setup = true;
            PlayerPrefs.SetFloat(ipdSaveKey, ipdOffset);
        }
    }
}
