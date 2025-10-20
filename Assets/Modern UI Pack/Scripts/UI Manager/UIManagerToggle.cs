using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Michsky.MUIP
{
    [ExecuteInEditMode]
    public class UIManagerToggle : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private UIManager UIManagerAsset;

        [Header("Resources")]
        [SerializeField] private Image border;
        [SerializeField] private Image background;
        [SerializeField] private Image check;
        [SerializeField] private TextMeshProUGUI onLabel;
        [SerializeField] private TextMeshProUGUI offLabel;

        void Awake()
        {
            if (UIManagerAsset == null) { UIManagerAsset = Resources.Load<UIManager>("MUIP Manager"); }

            this.enabled = true;

            if (!UIManagerAsset.enableDynamicUpdate)
            {
                UpdateToggle();
                this.enabled = false;
            }
        }

        void Update()
        {
            if (UIManagerAsset == null) { return; }
            if (UIManagerAsset.enableDynamicUpdate) { UpdateToggle(); }
        }

        void UpdateToggle()
        {
            border.color = UIManagerAsset.toggleBorderColor;
            background.color = UIManagerAsset.toggleBackgroundColor;
            check.color = UIManagerAsset.toggleCheckColor;
          
            if (onLabel != null)
            {
                onLabel.color = new Color(UIManagerAsset.toggleTextColor.r, UIManagerAsset.toggleTextColor.g, UIManagerAsset.toggleTextColor.b, onLabel.color.a);
                onLabel.font = UIManagerAsset.toggleFont;
            }

            if (offLabel != null)
            {
                offLabel.color = new Color(UIManagerAsset.toggleTextColor.r, UIManagerAsset.toggleTextColor.g, UIManagerAsset.toggleTextColor.b, offLabel.color.a);
                offLabel.font = UIManagerAsset.toggleFont;
            }
        }
    }
}