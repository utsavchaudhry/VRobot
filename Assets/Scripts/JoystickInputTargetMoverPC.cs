using UnityEngine;
using UnityEngine.UI;
using Michsky.MUIP;

public class JoystickInputTargetMoverPC : TargetMover
{
    [Space]

    [SerializeField] private GameObject lateralJoysticks;
    [SerializeField] private GameObject middleJoysticks;

    [Space]

    [SerializeField] private Button[] modeButtons;

    [Space]

    [SerializeField] private SliderManager leftSlider;
    [SerializeField] private SliderManager rightSlider;

    [Space]

    [Header("Z Smoothing")]
    [Tooltip("Time (seconds) to reach ~63% of the step; ~4.6×smoothTime to get within ~1% (critically-damped model).")]
    [SerializeField, Min(0.0001f)] private float smoothTime = 0.08f;

    [Tooltip("Optional cap on Z speed (units/second). Use Mathf.Infinity for no cap.")]
    [SerializeField] private float maxSpeedZ = 1;

    [Tooltip("Use localPosition instead of world position (recommended if targets are under moving parents).")]
    [SerializeField] private bool useLocalSpace = false;

    private enum State { Wheels, Camera, Arms }
    private State currentState;
    private float leftX, leftY, rightX, rightY, middleX, middleY;
    private float forwardSpeed;
    private bool buttonsConfigured;

    private float leftSliderValue, rightSliderValue;

    private float _leftVelZ = 0f;
    private float _rightVelZ = 0f;

    protected override void Start()
    {
        base.Start();
        FreezeCamera = currentState != State.Camera;
        ChangeControlMode(0);
        SetUpSlider();
    }

    private void SetUpSlider()
    {
        if (leftSlider)
        {
            leftSliderValue = leftSlider.mainSlider.value;
            leftSlider.mainSlider.onValueChanged.AddListener(LeftSliderUpdate);
        }

        if (rightSlider)
        {
            rightSliderValue = rightSlider.mainSlider.value;
            rightSlider.mainSlider.onValueChanged.AddListener(RightSliderUpdate);
        }
    }

    private void LeftSliderUpdate(float value)
    {
        leftSliderValue = value;
    }

    private void RightSliderUpdate(float value)
    {
        rightSliderValue = value;
    }

    private void ToggleJoystick()
    {
        if (currentState == State.Arms)
        {
            ToggleJoystick(lateralJoysticks, true);
        }
        else
        {
            ToggleJoystick(middleJoysticks, true);
        }
    }

    private void ToggleJoystick(GameObject joystick, bool state)
    {
        if (!joystick)
        {
            return;
        }

        joystick.SetActive(state);

        if (joystick == lateralJoysticks)
        {
            if (middleJoysticks)
            {
                middleJoysticks.SetActive(!state);
            }
        }
        else if (lateralJoysticks)
        {
            lateralJoysticks.SetActive(!state);
        }
    }

    protected override void UpdateStatusUI()
    {
        for (int i = 0; i < modeButtons.Length; i++)
        {
            modeButtons[i].interactable = i != (int)currentState;
            if (!buttonsConfigured)
            {
                int index = i;
                modeButtons[i].onClick.AddListener(() => ChangeControlMode(index));
            }
        }

        buttonsConfigured = true;

        if (!status)
        {
            return;
        }

        status.text = status.text = string.Format("Control Mode:\n<color=green>{0}", currentState.ToString());
    }

    private void ChangeControlMode(int mode)
    {
        if (IsMovementFrozen() || (int)currentState == mode)
        {
            //return;
        }

        ResetDamping();

        currentState = (State)mode;

        FreezeCamera = currentState != State.Camera;

        UpdateStatusUI();

        ToggleJoystick();
    }

    private void Update()
    {
        if (IsMovementFrozen())
        {
            //return;
        }

        if (currentState == State.Wheels)
        {
            middleX = UltimateJoystick.GetHorizontalAxis("Middle");
            middleY = UltimateJoystick.GetVerticalAxis("Middle");

            if (middleY < 0)
            {
                forwardSpeed = middleY * maxLinearSpeed * backwardSpeedPercentage;
            }
            else
            {
                forwardSpeed = middleY * maxLinearSpeed;
            }

            float turnSpeed = middleX * maxTurnSpeed;
            LeftWheelSpeed = forwardSpeed + turnSpeed;
            RightWheelSpeed = forwardSpeed - turnSpeed;
        }
        else
        {
            LeftWheelSpeed = RightWheelSpeed = 0f;
        }

        leftX = UltimateJoystick.GetHorizontalAxis("Left");
        leftY = UltimateJoystick.GetVerticalAxis("Left");
        rightX = UltimateJoystick.GetHorizontalAxis("Right");
        rightY = UltimateJoystick.GetVerticalAxis("Right");

        TranslateAndClamp(leftIkTarget, new Vector3(leftX, leftY, 0f), minLeftHandPosition, maxLeftHandPosition);
        TranslateAndClamp(rightIkTarget, new Vector3(rightX, rightY, 0f), minRightHandPosition, maxRightHandPosition);

        UpdateIKZOnlySmooth(
            leftIkTarget,
            minLeftHandPosition.z,
            maxLeftHandPosition.z,
            leftSliderValue,
            ref _leftVelZ
        );

        UpdateIKZOnlySmooth(
            rightIkTarget,
            minRightHandPosition.z,
            maxRightHandPosition.z,
            rightSliderValue,
            ref _rightVelZ
        );
    }

    private void UpdateIKZOnlySmooth(
        Transform target,
        float minZ,
        float maxZ,
        float slider01,
        ref float velZ
    )
    {
        if (target == null) return;

        // 1) Map slider ∈ [0,1] to target Z range (handles minZ > maxZ as well).
        float t = Mathf.Clamp01(slider01);
        float targetZ = Mathf.Lerp(minZ, maxZ, t);

        // 2) Read current Z in chosen space.
        Vector3 p = useLocalSpace ? target.localPosition : target.position;
        float currentZ = p.z;

        // 3) Critically-damped step towards targetZ (only Z axis).
        float newZ = Mathf.SmoothDamp(
            currentZ,
            targetZ,
            ref velZ,
            smoothTime,
            maxSpeedZ,
            Time.deltaTime
        );

        // 4) Write back Z, preserving X and Y.
        p.z = newZ;
        if (useLocalSpace) target.localPosition = p;
        else target.position = p;
    }

    /// <summary>
    /// If you change ranges at runtime and want to avoid “spring” from stale velocity,
    /// call this to reset damping state (e.g., on range reconfiguration).
    /// </summary>
    public void ResetDamping()
    {
        _leftVelZ = 0f;
        _rightVelZ = 0f;
    }
}
