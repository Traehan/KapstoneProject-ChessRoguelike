using UnityEngine;
using System;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    [Header("Currency Settings")]
    [SerializeField] private int startingCoins = 0;
    [SerializeField] private int encounterVictoryReward = 75;

    private int currentCoins;

    public int CurrentCoins
    {
        get => currentCoins;
        private set
        {
            currentCoins = Mathf.Max(0, value);
            OnCoinsChanged?.Invoke(currentCoins);
            SaveCoins();
        }
    }

    public event Action<int> OnCoinsChanged;

    private const string COINS_SAVE_KEY = "PlayerCoins";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadCoins();
    }

    public void ResetCurrency()
    {
        CurrentCoins = startingCoins;
        Debug.Log($"[CurrencyManager] Currency reset to {startingCoins} coins");
    }

    public void AddCoins(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning($"[CurrencyManager] Attempted to add negative coins: {amount}");
            return;
        }
        CurrentCoins += amount;
        Debug.Log($"[CurrencyManager] Added {amount} coins. Total: {CurrentCoins}");
    }

    public bool SpendCoins(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning($"[CurrencyManager] Attempted to spend negative coins: {amount}");
            return false;
        }

        if (CurrentCoins >= amount)
        {
            CurrentCoins -= amount;
            Debug.Log($"[CurrencyManager] Spent {amount} coins. Remaining: {CurrentCoins}");
            return true;
        }

        Debug.Log($"[CurrencyManager] Not enough coins. Need {amount}, have {CurrentCoins}");
        return false;
    }

    public bool CanAfford(int amount)
    {
        return CurrentCoins >= amount;
    }

    public void AwardEncounterVictory()
    {
        AddCoins(encounterVictoryReward);
        Debug.Log($"[CurrencyManager] Awarded {encounterVictoryReward} coins for encounter victory!");
    }

    public int GetEncounterReward()
    {
        return encounterVictoryReward;
    }

    private void SaveCoins()
    {
        PlayerPrefs.SetInt(COINS_SAVE_KEY, currentCoins);
        PlayerPrefs.Save();
    }

    private void LoadCoins()
    {
        if (PlayerPrefs.HasKey(COINS_SAVE_KEY))
        {
            currentCoins = PlayerPrefs.GetInt(COINS_SAVE_KEY);
            OnCoinsChanged?.Invoke(currentCoins);
            Debug.Log($"[CurrencyManager] Loaded {currentCoins} coins from save");
        }
        else
        {
            currentCoins = startingCoins;
            OnCoinsChanged?.Invoke(currentCoins);
            Debug.Log($"[CurrencyManager] No save found, starting with {startingCoins} coins");
        }
    }

    public static void ClearSavedCurrency()
    {
        PlayerPrefs.DeleteKey(COINS_SAVE_KEY);
        PlayerPrefs.Save();
        Debug.Log("[CurrencyManager] Cleared saved currency");
    }
}
