using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

namespace Michsky.MUIP
{
    public class ProgressBar : MonoBehaviour
    {
        // Content
        public float currentPercent;
        [Range(0, 100)] public int speed;
        public float minValue = 0;
        public float maxValue = 100;
        public float valueLimit = 100;

        // Resources
        public Image loadingBar;
        public TextMeshProUGUI textPercent;

        // Settings
        public bool isOn;
        public bool restart;
        public bool invert;
        public bool addPrefix;
        public bool addSuffix = true;
        public string prefix = "";
        public string suffix = "%";
        public bool isLooped = false;
        [Range(0, 5)] public int decimals = 0;

        // Events
        [System.Serializable]
        public class ProgressBarEvent : UnityEvent<float> { }
        public ProgressBarEvent onValueChanged;
        [HideInInspector] public Slider eventSource;

        void Start()
        {
            UpdateUI();
            InitializeEvents();
        }

        void Update()
        {
            if (!isOn)
                return;

            if (currentPercent <= maxValue && !invert) { currentPercent += speed * Time.unscaledDeltaTime; }
            else if (currentPercent >= minValue && invert) { currentPercent -= speed * Time.unscaledDeltaTime; }

            if (currentPercent >= maxValue && speed != 0 && restart && !invert) { currentPercent = 0; }
            else if (currentPercent <= minValue && speed != 0 && restart && invert) { currentPercent = maxValue; }
            else if (currentPercent >= maxValue && speed != 0 && !restart && !invert) { currentPercent = maxValue; }
            else if (currentPercent <= minValue && speed != 0 && !restart && invert) { currentPercent = minValue; }

            UpdateUI();
        }

        public void UpdateUI()
        {
            loadingBar.fillAmount = currentPercent / maxValue;

            if (addSuffix) { textPercent.text = currentPercent.ToString("F" + decimals) + suffix; }
            else { textPercent.text = currentPercent.ToString("F" + decimals); }

            if (addPrefix) { textPercent.text = prefix + textPercent.text; }
            if (eventSource != null) { eventSource.value = currentPercent; }
        }

        public void InitializeEvents()
        {
            if (Application.isPlaying && onValueChanged.GetPersistentEventCount() != 0)
            {
                if (eventSource == null) { eventSource = gameObject.AddComponent(typeof(Slider)) as Slider; }
                eventSource.transition = Selectable.Transition.None;
                eventSource.minValue = minValue;
                eventSource.maxValue = maxValue;
                eventSource.onValueChanged.AddListener(onValueChanged.Invoke);
            }
        }

        public void ClearEvents()
        {
            eventSource.onValueChanged.RemoveAllListeners();
        }

        // Will be replaced in future versions
        public void ChangeValue(float newValue)
        {
            currentPercent = newValue;
            UpdateUI();
        }

        public void SetValue(float newValue)
        {
            currentPercent = newValue;
            UpdateUI();
        }

        public void SetValue(float newValue, string newPrefix = null, string newSuffix = null, bool updateUI = true)
        {
            currentPercent = newValue;
            prefix = newPrefix;
            suffix = newSuffix;
            if (updateUI) { UpdateUI(); }
        }
    }
}