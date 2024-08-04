using UnityEngine;

public class VRobot : MonoBehaviour
{
    [SerializeField] private Transform head;
    [SerializeField] private Vector3 offset;

    private void Start()
    {
        InputManager.OnPrimaryButtonDown += ResetHead;
    }

    private void OnDestroy()
    {
        InputManager.OnPrimaryButtonDown -= ResetHead;
    }

    private void Update()
    {
        FollowPlayer();

        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetHead();
        }
    }

    private void FollowPlayer()
    {
        if (!head)
        {
            return;
        }

        transform.position = head.position + offset;
    }

    private void ResetHead()
    {
        if (!head)
        {
            return;
        }

        transform.eulerAngles = Vector3.up * head.eulerAngles.y;

        if (ServoMapper.Instance)
        {
            ServoMapper.Instance.SetYawOffset(-head.eulerAngles.y);
        }
    }
}
