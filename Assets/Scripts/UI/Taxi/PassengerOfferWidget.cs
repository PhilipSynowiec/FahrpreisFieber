using TMPro;
using UnityEngine;

public class PassengerOfferWidget : MonoBehaviour
{
    [SerializeField] private TMP_Text distanceText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private RectTransform card;
    [SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.8f, 0f);

    public void Bind(Transform target, PassengerJob job)
    {
        followTarget = target;
        if (distanceText != null) distanceText.text = $"{job.distanceWorld:0}m";
        if (timeText != null) timeText.text = $"{job.timeLimitSeconds:0}s";
        if (priceText != null) priceText.text = job.quotedPriceCoins.ToString();
    }

    private void LateUpdate()
    {
        if (card == null) return;
        var camera = Camera.main;
        if (followTarget == null || camera == null) { card.gameObject.SetActive(false); return; }
        Vector3 screen = camera.WorldToScreenPoint(followTarget.position + worldOffset);
        card.gameObject.SetActive(screen.z > 0f);
        if (screen.z <= 0f) return;
        // Screen-space overlay rendering cannot be occluded by buildings or other world geometry.
        var canvasRect = (RectTransform)transform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local);
        Vector2 half = card.rect.size * 0.5f;
        Rect bounds = canvasRect.rect;
        local.x = Mathf.Clamp(local.x, bounds.xMin + half.x + 12f, bounds.xMax - half.x - 12f);
        local.y = Mathf.Clamp(local.y, bounds.yMin + half.y + 12f, bounds.yMax - half.y - 12f);
        card.anchoredPosition = local;
    }
}
