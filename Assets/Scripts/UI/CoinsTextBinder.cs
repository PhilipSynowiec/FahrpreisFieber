using TMPro;
using UnityEngine;

public class CoinsTextBinder : MonoBehaviour
{
    [SerializeField] private TMP_Text label;

    private void Reset()
    {
        label = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        if (CurrencyManager.Instance == null) return;
        label.text = CurrencyManager.Instance.Coins.ToString();
    }
}
