using TMPro;
using UnityEngine;

public class ToastMessageUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text text;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Timing")]
    [SerializeField] private float showSeconds = 4f;
    [SerializeField] private float fadeSeconds = 0.25f;

    private float hideAt;
    private bool showing;

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        HideImmediate();
    }

    private void Update()
    {
        if (!showing) return;

        float t = Time.time;

        if (t >= hideAt)
        {
            float fadeT = Mathf.Clamp01((t - hideAt) / Mathf.Max(0.001f, fadeSeconds));
            SetAlpha(1f - fadeT);

            if (fadeT >= 1f)
                HideImmediate();
        }
    }

    public void Show(string message)
    {
        if (text != null) text.text = message;

        showing = true;
        hideAt = Time.time + showSeconds;

        SetAlpha(1f);
    }

    private void HideImmediate()
    {
        showing = false;
        SetAlpha(0f);
    }

    private void SetAlpha(float a)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = a;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }
}
