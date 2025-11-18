using UnityEngine;
using TMPro;

public class CurrencyDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private string textFormat = "Coins: {0}";

    void Start()
    {
        if (coinText == null)
        {
            coinText = GetComponent<TextMeshProUGUI>();
        }

        if (coinText == null)
        {
            Debug.LogError("[CurrencyDisplay] No TextMeshProUGUI component found!");
            enabled = false;
            return;
        }

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCoinsChanged += UpdateDisplay;
            UpdateDisplay(CurrencyManager.Instance.CurrentCoins);
        }
        else
        {
            Debug.LogWarning("[CurrencyDisplay] CurrencyManager not found! Retrying...");
            Invoke(nameof(RetryFindCurrencyManager), 0.5f);
        }
    }

    void RetryFindCurrencyManager()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCoinsChanged += UpdateDisplay;
            UpdateDisplay(CurrencyManager.Instance.CurrentCoins);
        }
        else
        {
            UpdateDisplay(0);
        }
    }

    void OnDestroy()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCoinsChanged -= UpdateDisplay;
        }
    }

    void UpdateDisplay(int coins)
    {
        if (coinText != null)
        {
            coinText.text = string.Format(textFormat, coins);
        }
    }
}
