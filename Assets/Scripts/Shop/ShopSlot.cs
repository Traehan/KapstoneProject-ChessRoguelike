using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Chess;

public class ShopSlot : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Button buyButton;
    [SerializeField] private GameObject emptySlotIndicator;

    private PieceUpgradeSO upgradeItem;
    private int price;
    private ShopManager shopManager;
    private bool isPurchased = false;

    void Awake()
    {
        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuyClicked);
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
        upgradeItem = null;
        price = 0;
        shopManager = null;
        isPurchased = false;

        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }

        if (priceText != null)
            priceText.text = "";

        if (emptySlotIndicator != null)
            emptySlotIndicator.SetActive(true);

        if (buyButton != null)
            buyButton.interactable = false;
    }

    void UpdateDisplay()
    {
        if (emptySlotIndicator != null)
            emptySlotIndicator.SetActive(false);

        if (upgradeItem != null)
        {
            if (itemIcon != null)
            {
                itemIcon.sprite = upgradeItem.icon;
                itemIcon.enabled = (upgradeItem.icon != null);
            }

            if (priceText != null)
                priceText.text = price.ToString();
        }

        UpdateBuyButton();
    }

    void UpdateBuyButton()
    {
        if (buyButton == null)
            return;

        if (isPurchased)
        {
            buyButton.interactable = false;

            if (priceText != null)
                priceText.text = "SOLD";

            return;
        }

        bool canAfford = CurrencyManager.Instance != null && CurrencyManager.Instance.CanAfford(price);
        bool hasItem = upgradeItem != null;

        buyButton.interactable = canAfford && hasItem;
    }

    void OnBuyClicked()
    {
        if (isPurchased)
            return;

        if (shopManager == null)
            return;

        if (upgradeItem != null)
            shopManager.OpenUpgradeDetails(this, upgradeItem, price);
    }

    public PieceUpgradeSO GetUpgradeItem() => upgradeItem;
    public int GetPrice() => price;

    public void MarkSoldAndClear()
    {
        isPurchased = true;

        if (buyButton != null)
            buyButton.interactable = false;

        gameObject.SetActive(false);
    }

    void OnEnable()
    {
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCoinsChanged += OnCoinsChanged;
    }

    void OnDisable()
    {
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCoinsChanged -= OnCoinsChanged;
    }

    void OnCoinsChanged(int newAmount)
    {
        UpdateBuyButton();
    }
}