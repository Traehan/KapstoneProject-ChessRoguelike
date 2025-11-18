using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Chess;

public class ShopSlot : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Button buyButton;
    [SerializeField] private GameObject emptySlotIndicator;

    private PieceDefinition pieceItem;
    private PieceUpgradeSO upgradeItem;
    private int price;
    private ShopManager shopManager;
    private bool isPurchased = false;

    void Awake()
    {
        if (buyButton != null)
        {
            buyButton.onClick.AddListener(OnBuyClicked);
        }
    }

    public void SetPieceItem(PieceDefinition piece, int cost, ShopManager manager)
    {
        Clear();
        pieceItem = piece;
        price = cost;
        shopManager = manager;
        UpdateDisplay();
    }

    public void SetUpgradeItem(PieceUpgradeSO upgrade, int cost, ShopManager manager)
    {
        Clear();
        upgradeItem = upgrade;
        price = cost;
        shopManager = manager;
        UpdateDisplay();
    }

    public void Clear()
    {
        pieceItem = null;
        upgradeItem = null;
        price = 0;
        isPurchased = false;
        
        if (itemIcon != null) itemIcon.sprite = null;
        if (itemNameText != null) itemNameText.text = "";
        if (itemDescriptionText != null) itemDescriptionText.text = "";
        if (priceText != null) priceText.text = "";
        
        if (emptySlotIndicator != null)
        {
            emptySlotIndicator.SetActive(true);
        }
        
        if (buyButton != null)
        {
            buyButton.interactable = false;
        }
    }

    void UpdateDisplay()
    {
        if (emptySlotIndicator != null)
        {
            emptySlotIndicator.SetActive(false);
        }

        if (pieceItem != null)
        {
            if (itemIcon != null) itemIcon.sprite = pieceItem.icon;
            if (itemNameText != null) itemNameText.text = pieceItem.displayName;
            if (itemDescriptionText != null) itemDescriptionText.text = $"HP: {pieceItem.maxHP} | ATK: {pieceItem.attack}";
            if (priceText != null) priceText.text = price.ToString();
        }
        else if (upgradeItem != null)
        {
            if (itemIcon != null) itemIcon.sprite = upgradeItem.icon;
            if (itemNameText != null) itemNameText.text = upgradeItem.displayName;
            if (itemDescriptionText != null) itemDescriptionText.text = upgradeItem.description;
            if (priceText != null) priceText.text = price.ToString();
        }

        UpdateBuyButton();
    }

    void UpdateBuyButton()
    {
        if (buyButton == null) return;

        if (isPurchased)
        {
            buyButton.interactable = false;
            if (priceText != null) priceText.text = "SOLD";
            return;
        }

        bool canAfford = CurrencyManager.Instance != null && CurrencyManager.Instance.CanAfford(price);
        bool hasItem = pieceItem != null || upgradeItem != null;
        
        buyButton.interactable = canAfford && hasItem;
    }

    void OnBuyClicked()
    {
        if (isPurchased) return;

        if (CurrencyManager.Instance == null)
        {
            Debug.LogError("[ShopSlot] CurrencyManager not found!");
            return;
        }

        if (!CurrencyManager.Instance.CanAfford(price))
        {
            Debug.Log("[ShopSlot] Cannot afford this item");
            return;
        }

        if (pieceItem != null)
        {
            if (shopManager != null && shopManager.OnPiecePurchased(pieceItem, price))
            {
                isPurchased = true;
                UpdateBuyButton();
            }
        }
        else if (upgradeItem != null)
        {
            if (shopManager != null && shopManager.OnUpgradePurchased(upgradeItem, price))
            {
                isPurchased = true;
                UpdateBuyButton();
            }
        }
    }

    void OnEnable()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCoinsChanged += OnCoinsChanged;
        }
    }

    void OnDisable()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCoinsChanged -= OnCoinsChanged;
        }
    }

    void OnCoinsChanged(int newAmount)
    {
        UpdateBuyButton();
    }
}
