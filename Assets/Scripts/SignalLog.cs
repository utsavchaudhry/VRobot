using UnityEngine;
using TMPro;

public class SignalLog : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI idText;
    [SerializeField] private TextMeshProUGUI positionText;

    private void Update()
    {
        idText.text = (transform.GetSiblingIndex() + 1).ToString();
    }

    public void SetPosition(string position = "NA")
    {
        positionText.text = position;
    }
}
