// Assets/Scripts/Shop/ShopManager.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Chess;
using GameManager;

public class ShopManager : MonoBehaviour
{
    [Header("Shop Slots")]
    [SerializeField] private ShopSlot[] pieceSlots;
    [SerializeField] private ShopSlot[] upgradeSlots;

    [Header("Available Items")]
    [SerializeField] private List<PieceDefinition> availablePieces;
    [SerializeField] private List<PieceUpgradeSO> availableUpgrades;

    [Header("Pricing")]
    [SerializeField] private int piecePriceMin = 50;
    [SerializeField] private int piecePriceMax = 150;
    [SerializeField] private int upgradePriceMin = 30;
    [SerializeField] private int upgradePriceMax = 100;
    [SerializeField] private int refreshCost = 25;

    [Header("UI References")]
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private PrepPanel prepPanel;

    [Header("Scene Navigation")]
    [SerializeField] private string mapSceneName = "MapScene";

    private List<PieceUpgradeSO> shownUpgrades = new List<PieceUpgradeSO>();
    private bool hasRefreshed = false;
    [SerializeField] bool useGameSessionStartingPool = true;

    void Start()
    {
        if (refreshButton != null) refreshButton.onClick.AddListener(OnRefreshClicked);
        if (leaveButton != null)   leaveButton.onClick.AddListener(OnLeaveClicked);
        PopulateShop();
    }

    void PopulateShop()
    {
        PopulatePieceSlots();
        PopulateUpgradeSlots();
        UpdateRefreshButton();
    }

    void PopulatePieceSlots()
    {
        if (pieceSlots == null || pieceSlots.Length == 0) return;

        // ✅ Pull from GameSession clan pool
        List<PieceDefinition> source;
        if (useGameSessionStartingPool && GameSession.I != null && GameSession.I.startingTroopPool != null)
            source = new List<PieceDefinition>(GameSession.I.startingTroopPool);
        else
            source = new List<PieceDefinition>(availablePieces); // fallback

        // Safety: remove nulls + duplicates
        source.RemoveAll(x => x == null);

        // (Optional) remove Queen / leader if it shouldn't appear in shop:
        // source.RemoveAll(x => x != null && x.isLeader); // depending on your fields

        ShuffleList(source);

        for (int i = 0; i < pieceSlots.Length; i++)
        {
            if (i < source.Count)
            {
                PieceDefinition def = source[i];
                int price = def.shopPrice;
                pieceSlots[i].SetPieceItem(def, price, manager: this);
            }
            else
            {
                pieceSlots[i].Clear();
            }
        }
    }

    void PopulateUpgradeSlots()
    {
        if (upgradeSlots == null || upgradeSlots.Length == 0) return;

        List<PieceUpgradeSO> availableForDisplay = new List<PieceUpgradeSO>(availableUpgrades);
        foreach (var shown in shownUpgrades) availableForDisplay.Remove(shown);
        ShuffleList(availableForDisplay);

        for (int i = 0; i < upgradeSlots.Length; i++)
        {
            if (i < availableForDisplay.Count)
            {
                PieceUpgradeSO upgrade = availableForDisplay[i];
                // use the cost defined on the ScriptableObject
                int price = upgrade.shopPrice;
                upgradeSlots[i].SetUpgradeItem(availableForDisplay[i], price, this);

                if (!shownUpgrades.Contains(availableForDisplay[i]))
                    shownUpgrades.Add(availableForDisplay[i]);
            }
            else upgradeSlots[i].Clear();
        }
    }

    void OnRefreshClicked()
    {
        if (hasRefreshed) { Debug.Log("[ShopManager] Already refreshed once this visit"); return; }
        if (CurrencyManager.Instance == null) { Debug.LogError("[ShopManager] CurrencyManager not found!"); return; }
        if (!CurrencyManager.Instance.CanAfford(refreshCost)) { Debug.Log("[ShopManager] Cannot afford refresh"); return; }

        if (CurrencyManager.Instance.SpendCoins(refreshCost))
        {
            hasRefreshed = true;
            PopulateUpgradeSlots();
            UpdateRefreshButton();
            Debug.Log("[ShopManager] Shop refreshed!");
        }
    }

    void UpdateRefreshButton()
    {
        if (refreshButton == null) return;

        if (hasRefreshed)
        {
            refreshButton.interactable = false;
            var buttonText = refreshButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (buttonText != null) buttonText.text = "USED";
        }
        else
        {
            bool canAfford = CurrencyManager.Instance != null && CurrencyManager.Instance.CanAfford(refreshCost);
            refreshButton.interactable = canAfford;
        }
    }

    public bool OnPiecePurchased(PieceDefinition piece, int cost)
    {
        if (CurrencyManager.Instance == null) return false;

        if (CurrencyManager.Instance.SpendCoins(cost))
        {
            var gs = GameSession.I;
            if (gs != null)
            {
                var runtimePiece = gs.CreateRuntimePiece(piece); // unique per slot
                gs.army.Add(runtimePiece);

                Debug.Log($"[ShopManager] Purchased {runtimePiece.displayName} for {cost} coins. Added to army.");

                if (prepPanel != null)
                {
                    prepPanel.gameObject.SetActive(false);
                    prepPanel.gameObject.SetActive(true);
                }
                return true;
            }
            else
            {
                Debug.LogError("[ShopManager] GameSession not found! Cannot add piece to army.");
                CurrencyManager.Instance.AddCoins(cost);
                return false;
            }
        }
        return false;
    }

    public bool OnUpgradePurchased(PieceUpgradeSO upgrade, int cost)
    {
        if (CurrencyManager.Instance == null) return false;
        var gs = GameSession.I;
        if (gs == null || gs.army == null || gs.army.Count == 0)
        {
            Debug.LogError("[ShopManager] No army pieces available to upgrade!");
            return false;
        }

        // Filter eligible pieces (not Queen + has free slots)
        List<PieceDefinition> eligiblePieces = gs.army
            .Where(p =>
                p != null &&
                p.displayName != null &&
                !p.displayName.Contains("Queen") &&
                gs.GetUpgradeCount(p) < gs.GetUpgradeSlotsMax(p))
            .ToList();

        if (eligiblePieces.Count == 0)
        {
            Debug.LogError("[ShopManager] No eligible pieces to upgrade (either Queen or all slots full)!");
            return false;
        }

        UpgradeSelectionPopup.ShowPopup(eligiblePieces, upgrade, cost, (selectedPiece) =>
        {
            if (selectedPiece == null) return;
            var gsInner = GameSession.I;
            if (gsInner == null) return;

            int used = gsInner.GetUpgradeCount(selectedPiece);
            int maxSlots = gsInner.GetUpgradeSlotsMax(selectedPiece);
            if (used >= maxSlots)
            {
                Debug.Log($"[ShopManager] {selectedPiece.displayName} has no free upgrade slots ({used}/{maxSlots}).");
                return;
            }

            if (CurrencyManager.Instance.SpendCoins(cost))
            {
                ApplyUpgradeToPiece(selectedPiece, upgrade);
                gsInner.IncrementUpgradeCount(selectedPiece);

                Debug.Log($"[ShopManager] Applied {upgrade.displayName} to {selectedPiece.displayName} ({used + 1}/{maxSlots} slots used)");

                if (prepPanel != null)
                {
                    prepPanel.gameObject.SetActive(false);
                    prepPanel.gameObject.SetActive(true);
                }
            }
        });

        return true;
    }

    void ApplyUpgradeToPiece(PieceDefinition piece, PieceUpgradeSO upgrade)
    {
        // (Optional) preview numbers on the definition for UI
        piece.maxHP += upgrade.addMaxHP;
        piece.attack += upgrade.addAttack;

        // 🔹 NEW: queue this upgrade so the spawned instance gets the keyword behavior/stat in battle
        GameSession.I?.QueueUpgrade(piece, upgrade);

        Debug.Log($"[ShopManager] {piece.displayName} upgraded! (preview) HP: {piece.maxHP}, ATK: {piece.attack} — queued '{upgrade.displayName}' for next spawn.");
    }

    void OnLeaveClicked()
    {
        if (SceneController.instance != null) SceneController.instance.GoTo(mapSceneName);
        else Debug.LogError("[ShopManager] SceneController not found!");
    }

    void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
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

    void OnCoinsChanged(int newAmount) => UpdateRefreshButton();
}
