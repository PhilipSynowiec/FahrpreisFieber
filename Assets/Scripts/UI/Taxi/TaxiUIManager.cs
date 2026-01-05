using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TaxiUIManager : MonoBehaviour
{
    [Header("Result Popup (left)")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultRatingText;
    [SerializeField] private TMP_Text resultTimeText;
    [SerializeField] private TMP_Text resultPayoutText;
    [SerializeField] private Button resultCloseButton;

    public bool IsModalOpen => resultPanel != null && resultPanel.activeSelf;

    private System.Action onResultClose;

    private void Awake()
    {
        if (resultCloseButton != null)
            resultCloseButton.onClick.AddListener(() => onResultClose?.Invoke());

        HideResult();
    }

    public void ShowResult(TripResult result, System.Action onClose)
    {
        if (resultPanel == null) return;

        onResultClose = onClose;
        resultPanel.SetActive(true);

        if (resultRatingText != null) resultRatingText.text = result.onTime ? "gut" : "schlecht";
        if (resultTimeText != null) resultTimeText.text = $"{result.elapsedSeconds:0}s / {result.timeLimitSeconds:0}s";
        if (resultPayoutText != null) resultPayoutText.text = $"{result.payoutCoins}";
    }

    public void HideResult()
    {
        if (resultPanel != null) resultPanel.SetActive(false);
        onResultClose = null;
    }
}
