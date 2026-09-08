using System;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }
    public static event Action<int> BalanceChanged;
    private const string CoinsKey = "coins";
    public int Coins { get; private set; }
    public static int Balance => Instance != null ? Instance.Coins : Mathf.Max(0, PlayerPrefs.GetInt(CoinsKey, 0));

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Instance = null; BalanceChanged = null; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Coins = Mathf.Max(0, PlayerPrefs.GetInt(CoinsKey, 0));
        BalanceChanged?.Invoke(Coins);
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    public bool TrySpend(int amount)
    {
        if (amount < 0 || Coins < amount) return false;
        AddCoins(-amount);
        return true;
    }

    public void AddCoins(int amount)
    {
        Coins = (int)Math.Max(0L, Math.Min(int.MaxValue, (long)Coins + amount));
        PlayerPrefs.SetInt(CoinsKey, Coins);
        PlayerPrefs.Save();
        BalanceChanged?.Invoke(Coins);
    }
}
