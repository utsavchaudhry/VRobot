using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;

namespace Michsky.MUIP
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public class NotificationManager : MonoBehaviour, IPointerClickHandler
    {
        // Content
        public Sprite icon;
        public string title = "Notification Title";
        [TextArea(1, 4)] public string description = "Notification description";

        // Resources
        public Animator notificationAnimator;
        public Image iconObj;
        public TextMeshProUGUI titleObj;
        public TextMeshProUGUI descriptionObj;

        // Settings
        public bool enableTimer = true;
        public float timer = 3f;
        [SerializeField] private bool useCustomContent = false;
        public bool closeOnClick = false;
        public bool useStacking = false;
        [HideInInspector] public bool isOn;
        public StartBehaviour startBehaviour = StartBehaviour.Disable;
        public CloseBehaviour closeBehaviour = CloseBehaviour.Disable;
        public SlideDirection slideDirection = SlideDirection.Default;

        // Events
        public UnityEvent onOpen = new UnityEvent();
        public UnityEvent onClose = new UnityEvent();

        public enum StartBehaviour { None, Disable, Open }
        public enum CloseBehaviour { None, Disable, Destroy }
        public enum SlideDirection { Default, Left, Right }

        void Awake()
        {
            isOn = false;

            if (!useCustomContent) { UpdateUI(); }
            if (notificationAnimator == null) { notificationAnimator = gameObject.GetComponent<Animator>(); }
            if (useStacking)
            {
                try { transform.GetComponentInParent<NotificationStacking>().AddToStack(this); }
                catch { Debug.LogError("<b>[Notification]</b> 'Stacking' is enabled but 'Notification Stacking' cannot be found in parent.", this); }
            }
        }

        void Start()
        {
            if (startBehaviour == StartBehaviour.Disable) { gameObject.SetActive(false); }
            else if (startBehaviour == StartBehaviour.Open) { Open(); }
        }

        public void Open()
        {
            if (isOn)
                return;

            gameObject.SetActive(true);
            isOn = true;

            StopCoroutine("StartTimer");
            StopCoroutine("DisableNotification");

            notificationAnimator.Play("In");
            onOpen.Invoke();

            if (enableTimer) 
            { 
                StartCoroutine("StartTimer"); 
            }
        }

        public void Close()
        {
            if (!isOn)
                return;

            isOn = false;
            notificationAnimator.Play("Out");
            onClose.Invoke();

            StopCoroutine("StartTimer");
            StopCoroutine("DisableNotification");
            StartCoroutine("DisableNotification");
        }

        #region Obsolete
        public void OpenNotification() { Open(); }
        public void CloseNotification() { Close(); }
        #endregion

        public void UpdateUI()
        {
            if (iconObj != null) { iconObj.sprite = icon; }
            if (titleObj != null) { titleObj.text = title; }
            if (descriptionObj != null) { descriptionObj.text = description; }

            if (slideDirection == SlideDirection.Left)
            {
                transform.localScale = new Vector3(-1, transform.localScale.y, transform.localScale.z);
                transform.GetChild(0).transform.localScale = new Vector3(-1, transform.GetChild(0).transform.localScale.y, transform.GetChild(0).transform.localScale.z);
            }

            else if (slideDirection == SlideDirection.Right)
            {
                transform.localScale = new Vector3(1, transform.localScale.y, transform.localScale.z);
                transform.GetChild(0).transform.localScale = new Vector3(1, transform.GetChild(0).transform.localScale.y, transform.GetChild(0).transform.localScale.z);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!closeOnClick)
                return;

            Close();
        }

        IEnumerator StartTimer()
        {
            yield return new WaitForSecondsRealtime(timer);
            Close();
        }

        IEnumerator DisableNotification()
        {
            yield return new WaitForSecondsRealtime(1f);

            if (closeBehaviour == CloseBehaviour.Disable) { gameObject.SetActive(false); isOn = false; }
            else if (closeBehaviour == CloseBehaviour.Destroy) { Destroy(gameObject); }
        }
    }
}