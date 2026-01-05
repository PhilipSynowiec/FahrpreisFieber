using TMPro;
using UnityEngine;

public class PassengerOfferWidget : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text distanceText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text priceText;

    [Header("Follow")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.2f, 0f);
    [SerializeField] private bool faceCamera = true;

    public void Bind(Transform target, PassengerJob job)
    {
        followTarget = target;

        if (distanceText) distanceText.text = $"{job.distanceWorld:0}m";
        if (timeText) timeText.text = $"{job.timeLimitSeconds:0}s";
        if (priceText) priceText.text = $"{job.quotedPriceCoins}";
    }

    private void LateUpdate()
    {
        if (followTarget == null) return;
        transform.position = followTarget.position + worldOffset;

        if (faceCamera && Camera.main != null)
            transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
    }
}
