using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }
    private const string CoinsKey = "coins";

    public int Coins { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Coins = PlayerPrefs.GetInt(CoinsKey, 0);
    }

    public void AddCoins(int amount)
    {
        Coins = Mathf.Max(0, Coins + amount);
        PlayerPrefs.SetInt(CoinsKey, Coins);
        PlayerPrefs.Save();
    }
}
