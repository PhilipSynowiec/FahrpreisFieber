using TMPro;
using UnityEngine;

public class CoinsTextBinder : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    private void Reset() => label = GetComponent<TMP_Text>();
    private void OnEnable()
    {
        if (label == null) label = GetComponent<TMP_Text>();
        CurrencyManager.BalanceChanged += Refresh;
        Refresh(CurrencyManager.Balance);
    }
    private void OnDisable() => CurrencyManager.BalanceChanged -= Refresh;
    private void Refresh(int coins) { if (label != null) label.text = $"{coins:N0}"; }
}
