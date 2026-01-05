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
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.6f, 0f);

    [Header("Billboard")]
    [SerializeField] private bool faceCamera = true;
    [Tooltip("Flip 180° if your canvas/text appears mirrored.")]
    [SerializeField] private bool flipY180 = true;

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

        if (!faceCamera || Camera.main == null) return;

        Vector3 toCam = Camera.main.transform.position - transform.position;
        toCam.y = 0f; // keep upright
        if (toCam.sqrMagnitude < 0.0001f) return;

        // Face camera
        transform.rotation = Quaternion.LookRotation(toCam);

        // Many world-space canvases are "backwards" -> flip so text isn't mirrored
        if (flipY180)
            transform.rotation *= Quaternion.Euler(0f, 180f, 0f);
    }
}
