using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TaxiHUDUI : MonoBehaviour
{
    [SerializeField] private Button acceptButton;
    [SerializeField] private Image actionIcon;
    [SerializeField] private Sprite pickupIcon;
    [SerializeField] private Sprite dropoffIcon;
    [SerializeField] private TMP_Text shiftStatus;
    [SerializeField] private TripManager trips;
    private System.Action onAccept;

    private void Update()
    {
        if (shiftStatus == null || trips == null) return;
        shiftStatus.text = trips.HasActiveTrip ? $"{Mathf.CeilToInt(trips.RemainingSeconds)}s" : string.Empty;
    }

    private void Awake()
    {
        if (acceptButton == null) return;
        acceptButton.onClick.AddListener(() => onAccept?.Invoke());
        acceptButton.interactable = false;
    }

    public void SetAccept(bool visible, bool enabled, bool droppingOff, System.Action onClick)
    {
        if (acceptButton == null) return;
        acceptButton.gameObject.SetActive(visible);
        acceptButton.interactable = enabled;
        if (actionIcon != null) actionIcon.sprite = droppingOff ? dropoffIcon : pickupIcon;
        onAccept = onClick;
    }
}
