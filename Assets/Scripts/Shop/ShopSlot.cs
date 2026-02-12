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
    [SerializeField] private TextMeshProUGUI innateAbilitiesText;
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
        if (innateAbilitiesText != null) innateAbilitiesText.text = "";
        
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
            if (innateAbilitiesText != null) innateAbilitiesText.text = BuildInnateAbilityText(pieceItem);
        }
        else if (upgradeItem != null)
        {
            if (itemIcon != null) itemIcon.sprite = upgradeItem.icon;
            if (itemNameText != null) itemNameText.text = upgradeItem.displayName;
            if (itemDescriptionText != null) itemDescriptionText.text = upgradeItem.description;
            if (priceText != null) priceText.text = price.ToString();
            if (innateAbilitiesText != null) innateAbilitiesText.text = ""; //upgrades do not have innate
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
    
    string BuildInnateAbilityText(PieceDefinition def)
    {
        if (def == null || def.piecePrefab == null)
            return "";

        // Try to read PieceLoadout from the prefab
        var loadout = def.piecePrefab.GetComponent<PieceLoadout>();
        if (loadout == null || loadout.innateAbilities == null || loadout.innateAbilities.Count == 0)
            return ""; // or "Innate: None"

        var abilities = loadout.innateAbilities;

        // If there is exactly 1 innate ability, show a compact block
        if (abilities.Count == 1)
        {
            var a = abilities[0];
            if (a == null) return "";

            string name = string.IsNullOrEmpty(a.displayName) ? a.name : a.displayName;
            return $"Innate: {name}\n{a.description}";
        }

        // Otherwise, list them all
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("Innate Abilities:");

        foreach (var a in abilities)
        {
            if (a == null) continue;
            string name = string.IsNullOrEmpty(a.displayName) ? a.name : a.displayName;
            if (!string.IsNullOrEmpty(a.description))
                sb.AppendLine($"• {name} — {a.description}");
            else
                sb.AppendLine($"• {name}");
        }

        return sb.ToString();
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
