// Assets/Scripts/Shop/ShopManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Chess;
using GameManager;

public class ShopManager : MonoBehaviour
{
    [Header("Shop Slots")]
    [SerializeField] private ShopUnitCardSlot unitSlot;
    [SerializeField] private ShopSlot[] upgradeSlots;

    [Header("Available Items")]
    [SerializeField] private List<PieceDefinition> availablePieces;
    [SerializeField] private List<PieceUpgradeSO> availableUpgrades;

    [Header("Pricing")]
    [SerializeField] private int refreshCost = 25;

    [Header("UI References")]
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private PrepPanel prepPanel;

    [Header("Upgrade Details Panel")]
    [SerializeField] private GameObject upgradeDetailsPanel;
    [SerializeField] private TMP_Text upgradeDetailsNameText;
    [SerializeField] private Image upgradeDetailsIconImage;
    [SerializeField] private TMP_Text upgradeDetailsDescriptionText;
    [SerializeField] private TMP_Text upgradeDetailsCostText;
    [SerializeField] private Button upgradeDetailsBuyButton;
    [SerializeField] private Button upgradeDetailsCloseButton;

    [Header("Scene Navigation")]
    [SerializeField] private string mapSceneName = "MapScene";

    [Header("Sources")]
    [SerializeField] private bool useGameSessionStartingPool = true;

    private readonly List<PieceUpgradeSO> shownUpgrades = new();
    private bool hasRefreshed = false;

    private PieceUpgradeSO _pendingViewedUpgrade;
    private ShopSlot _pendingViewedUpgradeSlot;
    private int _pendingViewedUpgradeCost;

    void Start()
    {
        if (refreshButton != null)
            refreshButton.onClick.AddListener(OnRefreshClicked);

        if (leaveButton != null)
            leaveButton.onClick.AddListener(OnLeaveClicked);

        if (upgradeDetailsBuyButton != null)
            upgradeDetailsBuyButton.onClick.AddListener(OnUpgradeDetailsBuyClicked);

        if (upgradeDetailsCloseButton != null)
            upgradeDetailsCloseButton.onClick.AddListener(CloseUpgradeDetails);

        if (upgradeDetailsPanel != null)
            upgradeDetailsPanel.SetActive(false);

        PopulateShop();
    }

    void PopulateShop()
    {
        PopulateUnitSlot();
        PopulateUpgradeSlots();
        UpdateRefreshButton();
    }

    void PopulateUnitSlot()
    {
        if (unitSlot == null)
            return;

        List<PieceDefinition> source;

        if (useGameSessionStartingPool && GameSession.I != null && GameSession.I.startingTroopPool != null)
            source = new List<PieceDefinition>(GameSession.I.startingTroopPool);
        else
            source = new List<PieceDefinition>(availablePieces);

        source.RemoveAll(x => x == null);

        // Remove queen if needed
        source.RemoveAll(IsQueenLikePiece);

        ShuffleList(source);

        if (source.Count > 0)
        {
            PieceDefinition def = source[0];
            int price = def.shopPrice;
            unitSlot.Bind(def, price, this);
        }
        else
        {
            unitSlot.Clear();
        }
    }

    void PopulateUpgradeSlots()
    {
        if (upgradeSlots == null || upgradeSlots.Length == 0)
            return;

        List<PieceUpgradeSO> availableForDisplay = new(availableUpgrades);

        foreach (var shown in shownUpgrades)
            availableForDisplay.Remove(shown);

        ShuffleList(availableForDisplay);

        for (int i = 0; i < upgradeSlots.Length; i++)
        {
            if (i < availableForDisplay.Count)
            {
                PieceUpgradeSO upgrade = availableForDisplay[i];
                int price = upgrade.shopPrice;

                upgradeSlots[i].SetUpgradeItem(upgrade, price, this);

                if (!shownUpgrades.Contains(upgrade))
                    shownUpgrades.Add(upgrade);
            }
            else
            {
                upgradeSlots[i].Clear();
            }
        }
    }

    void OnRefreshClicked()
    {
        if (hasRefreshed)
        {
            Debug.Log("[ShopManager] Already refreshed once this visit.");
            return;
        }

        if (CurrencyManager.Instance == null)
        {
            Debug.LogError("[ShopManager] CurrencyManager not found!");
            return;
        }

        if (!CurrencyManager.Instance.CanAfford(refreshCost))
        {
            Debug.Log("[ShopManager] Cannot afford refresh.");
            return;
        }

        if (CurrencyManager.Instance.SpendCoins(refreshCost))
        {
            hasRefreshed = true;
            PopulateUpgradeSlots();
            UpdateRefreshButton();
            Debug.Log("[ShopManager] Shop refreshed.");
        }
    }

    void UpdateRefreshButton()
    {
        if (refreshButton == null)
            return;

        var buttonText = refreshButton.GetComponentInChildren<TMP_Text>();

        if (hasRefreshed)
        {
            refreshButton.interactable = false;
            if (buttonText != null)
                buttonText.text = "USED";
        }
        else
        {
            bool canAfford = CurrencyManager.Instance != null && CurrencyManager.Instance.CanAfford(refreshCost);
            refreshButton.interactable = canAfford;

            if (buttonText != null)
                buttonText.text = "Refresh Upgrades";
        }
    }

    public bool OnPiecePurchased(PieceDefinition piece, int cost)
    {
        if (piece == null)
            return false;

        if (CurrencyManager.Instance == null)
            return false;

        if (!CurrencyManager.Instance.SpendCoins(cost))
            return false;

        var gs = GameSession.I;
        if (gs == null)
        {
            Debug.LogError("[ShopManager] GameSession not found! Cannot add piece to army.");
            CurrencyManager.Instance.AddCoins(cost);
            return false;
        }

        var runtimePiece = gs.CreateRuntimePiece(piece);
        gs.army.Add(runtimePiece);

        Debug.Log($"[ShopManager] Purchased {runtimePiece.displayName} for {cost} coins. Added to army.");

        RefreshPrepPanel();
        return true;
    }

    public void OpenUpgradeDetails(ShopSlot sourceSlot, PieceUpgradeSO upgrade, int cost)
    {
        if (sourceSlot == null || upgrade == null)
            return;

        _pendingViewedUpgrade = upgrade;
        _pendingViewedUpgradeSlot = sourceSlot;
        _pendingViewedUpgradeCost = cost;

        if (upgradeDetailsNameText != null)
            upgradeDetailsNameText.text = upgrade.displayName;

        if (upgradeDetailsIconImage != null)
        {
            upgradeDetailsIconImage.sprite = upgrade.icon;
            upgradeDetailsIconImage.enabled = (upgrade.icon != null);
        }

        if (upgradeDetailsDescriptionText != null)
            upgradeDetailsDescriptionText.text = upgrade.description;

        if (upgradeDetailsCostText != null)
            upgradeDetailsCostText.text = cost.ToString();

        if (upgradeDetailsBuyButton != null)
        {
            bool canAfford = CurrencyManager.Instance != null && CurrencyManager.Instance.CanAfford(cost);
            upgradeDetailsBuyButton.interactable = canAfford;
        }

        if (upgradeDetailsPanel != null)
            upgradeDetailsPanel.SetActive(true);
    }

    public void OnUpgradeDetailsBuyClicked()
    {
        if (_pendingViewedUpgrade == null)
            return;

        if (CurrencyManager.Instance == null)
            return;

        if (!CurrencyManager.Instance.CanAfford(_pendingViewedUpgradeCost))
        {
            Debug.Log("[ShopManager] Cannot afford upgrade.");
            return;
        }

        List<PieceDefinition> eligiblePieces = GetEligibleUpgradeTargets();
        if (eligiblePieces.Count == 0)
        {
            Debug.LogWarning("[ShopManager] No eligible army pieces available for this upgrade.");
            return;
        }

        if (DeckViewController.Instance == null)
        {
            Debug.LogWarning("[ShopManager] No DeckViewController found in scene.");
            return;
        }

        DeckViewController.Instance.OpenArmyUpgradeSelectionMode(
            selectablePieces: eligiblePieces,
            title: "Choose Upgrade Target",
            instruction: "Select 1 unit to receive this upgrade.",
            onConfirm: OnUpgradeTargetConfirmed,
            onCancel: OnUpgradeTargetSelectionCancelled
        );
    }

    void OnUpgradeTargetConfirmed(PieceDefinition selectedPiece)
    {
        if (selectedPiece == null)
            return;

        if (_pendingViewedUpgrade == null)
            return;

        if (CurrencyManager.Instance == null)
            return;

        int used = GameSession.I != null ? GameSession.I.GetUpgradeCount(selectedPiece) : 0;
        int maxSlots = GameSession.I != null ? GameSession.I.GetUpgradeSlotsMax(selectedPiece) : 0;

        if (used >= maxSlots)
        {
            Debug.Log($"[ShopManager] {selectedPiece.displayName} has no free upgrade slots ({used}/{maxSlots}).");
            return;
        }

        if (!CurrencyManager.Instance.SpendCoins(_pendingViewedUpgradeCost))
        {
            Debug.Log("[ShopManager] Purchase failed during final spend.");
            return;
        }

        ApplyUpgradeToPiece(selectedPiece, _pendingViewedUpgrade);
        GameSession.I?.IncrementUpgradeCount(selectedPiece);

        Debug.Log($"[ShopManager] Applied {_pendingViewedUpgrade.displayName} to {selectedPiece.displayName} ({used + 1}/{maxSlots} slots used)");

        if (_pendingViewedUpgradeSlot != null)
            _pendingViewedUpgradeSlot.MarkSoldAndClear();

        CloseUpgradeDetails();
        RefreshPrepPanel();
    }

    void OnUpgradeTargetSelectionCancelled()
    {
        // Intentionally do nothing.
        // Upgrade remains available and the details panel stays open unless you close it manually.
    }

    public void CloseUpgradeDetails()
    {
        if (upgradeDetailsPanel != null)
            upgradeDetailsPanel.SetActive(false);

        _pendingViewedUpgrade = null;
        _pendingViewedUpgradeSlot = null;
        _pendingViewedUpgradeCost = 0;
    }

    List<PieceDefinition> GetEligibleUpgradeTargets()
    {
        var gs = GameSession.I;
        List<PieceDefinition> result = new();

        if (gs == null || gs.army == null)
            return result;

        foreach (var piece in gs.army)
        {
            if (piece == null)
                continue;

            if (IsQueenLikePiece(piece))
                continue;

            if (IsPawnLikePiece(piece))
                continue;

            int used = gs.GetUpgradeCount(piece);
            int max = gs.GetUpgradeSlotsMax(piece);

            if (used < max)
                result.Add(piece);
        }

        return result;
    }

    bool IsQueenLikePiece(PieceDefinition piece)
    {
        if (piece == null || string.IsNullOrWhiteSpace(piece.displayName))
            return false;

        return piece.displayName.ToLowerInvariant().Contains("queen");
    }

    bool IsPawnLikePiece(PieceDefinition piece)
    {
        if (piece == null || string.IsNullOrWhiteSpace(piece.displayName))
            return false;

        return piece.displayName.ToLowerInvariant().Contains("pawn");
    }

    void ApplyUpgradeToPiece(PieceDefinition piece, PieceUpgradeSO upgrade)
    {
        if (piece == null || upgrade == null)
            return;

        // preview stats on the runtime piece definition in the army
        piece.maxHP += upgrade.addMaxHP;
        piece.attack += upgrade.addAttack;

        // queue upgrade so spawned piece instance gets the real runtime effect/hooks
        GameSession.I?.QueueUpgrade(piece, upgrade);

        Debug.Log($"[ShopManager] {piece.displayName} upgraded! HP: {piece.maxHP}, ATK: {piece.attack} — queued '{upgrade.displayName}' for battle spawn.");
    }

    void RefreshPrepPanel()
    {
        if (prepPanel != null)
        {
            prepPanel.gameObject.SetActive(false);
            prepPanel.gameObject.SetActive(true);
        }
    }

    void OnLeaveClicked()
    {
        if (SceneController.instance != null)
            SceneController.instance.GoTo(mapSceneName);
        else
            Debug.LogError("[ShopManager] SceneController not found!");
    }

    void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
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
        UpdateRefreshButton();

        if (_pendingViewedUpgrade != null && upgradeDetailsBuyButton != null)
            upgradeDetailsBuyButton.interactable = CurrencyManager.Instance != null &&
                                                   CurrencyManager.Instance.CanAfford(_pendingViewedUpgradeCost);
    }
}