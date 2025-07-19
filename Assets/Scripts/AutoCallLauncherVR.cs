using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Byn.Unity.Examples;

public class AutoCallLauncherVR : MonoBehaviour
{
    private CallAppUi callApp;

    [SerializeField] [Range(0f, 1f)] private float initDelay = 0.1f;
    [SerializeField] [Range(1f, 3f)] private float shutdownHoldTime = 1.5f;
    [SerializeField] private string roomName = "HopeJr";
    [SerializeField] private bool enableAudio = false;
    [SerializeField] private bool enableVideo = false;

    [Space]

    [Header("Call UI References")]

    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private InputField roomNameField;
    [SerializeField] private Toggle audioToggle;
    [SerializeField] private Toggle videoToggle;

    private float inactiveTimer;
    private float shutdownTimer;
    private bool callStarted;
    private bool shutdownTrigger;

    private void Start()
    {
        ResetState();

        if (InputManager.LeftController != null)
        {
            InputManager.LeftController.PrimaryBtn.OnDown += ShutdownTrigger;
            InputManager.LeftController.PrimaryBtn.OnUp += ShutdownCancel;
        }
    }

    private void OnDestroy()
    {
        if (InputManager.LeftController != null)
        {
            InputManager.LeftController.PrimaryBtn.OnDown -= ShutdownTrigger;
            InputManager.LeftController.PrimaryBtn.OnUp -= ShutdownCancel;
        }
    }

    private IEnumerator StartCall()
    {
        callStarted = true;

        yield return new WaitForSeconds(initDelay);

        if (roomNameField)
        {
            roomNameField.text = roomName;
        }

        if (audioToggle)
        {
            audioToggle.isOn = enableAudio;
        }

        if (videoToggle)
        {
            videoToggle.isOn = enableVideo;
        }

        if (!callApp)
        {
            callApp = FindObjectOfType<CallAppUi>();
        }

        callApp.JoinButtonPressed();
    }

    private void ShutdownTrigger()
    {
        shutdownTrigger = true;
    }

    private void ShutdownCancel()
    {
        shutdownTrigger = false;
    }

    private void ResetState()
    {
        callStarted = false;
        shutdownTrigger = false;
        inactiveTimer = 0f;
        shutdownTimer = 0f;
    }

    private void Update()
    {
        if (!settingsPanel)
        {
            return;
        }

        if (callStarted)
        {
            if (shutdownTrigger)
            {
                if (shutdownTimer > shutdownHoldTime)
                {
                    ResetState();
                    callApp.ShutdownButtonPressed();
                }
                else
                {
                    shutdownTimer += Time.deltaTime;
                }
            }
            else
            {
                shutdownTimer = 0f;

                if (settingsPanel.activeSelf)
                {
                    if (inactiveTimer > 5f)
                    {
                        ResetState();
                    }
                    inactiveTimer += Time.deltaTime;
                }
                else
                {
                    inactiveTimer = 0f;
                }
            }
        }
        else
        {
            if (settingsPanel.activeSelf)
            {
                _ = StartCoroutine(StartCall());
            }
        }
    }
}
