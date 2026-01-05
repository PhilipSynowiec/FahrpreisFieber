using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TaxiHUDUI : MonoBehaviour
{
    [SerializeField] private Button acceptButton;
    [SerializeField] private TMP_Text acceptButtonLabel;

    private System.Action onAccept;

    private void Awake()
    {
        if (acceptButton != null)
        {
            acceptButton.onClick.AddListener(() => onAccept?.Invoke());
            acceptButton.interactable = false;
        }
    }

    public void SetAccept(bool visible, bool enabled, string label, System.Action onClick)
    {
        if (acceptButton == null) return;

        acceptButton.gameObject.SetActive(visible);
        acceptButton.interactable = enabled;
        if (acceptButtonLabel != null) acceptButtonLabel.text = label;

        onAccept = onClick;
    }
}
