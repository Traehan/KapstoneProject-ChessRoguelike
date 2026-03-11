using System.Collections.Generic;
using UnityEngine;
using Chess;
using Card;

public class GameSession : MonoBehaviour
{
    public static GameSession I { get; private set; }

    [Header("Encounter Sources")]
    public EncounterCatalog encounterCatalog;

    [Header("Starting Troop Pool (map popup)")]
    public PieceDefinition[] startingTroopPool;

    [Header("Run State (runtime)")]
    public ClanDefinition selectedClan;
    public EncounterDefinition selectedEncounter;
    public List<PieceDefinition> army = new();

    [Header("Queen Icon (optional but recommended)")]
    public Sprite queenIconFallback;
    public GameObject queenIconPrefabOverride;

    [Header("Legacy Combat Deck (kept only for compatibility / migration)")]
    public PieceDefinition pawnTemplate;
    public List<PieceDefinition> runDeckNonLeaders = new();

    [Header("Card-Based Combat Deck (persistent for run)")]
    public List<CardDefinitionSO> CurrentRunDeck { get; private set; } = new();

    public bool hasGrantedStartingTroop = false;

    PieceDefinition _queenDefRuntime;

    readonly Dictionary<PieceDefinition, int> _upgradeCounts = new();
    readonly Dictionary<PieceDefinition, List<PieceUpgradeSO>> pendingUpgrades = new();

    [Header("Run / Boss State")]
    public bool isBossBattle;
    public bool bossDefeated;

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public void QueueUpgrade(PieceDefinition def, PieceUpgradeSO upg)
    {
        if (def == null || upg == null) return;

        if (!pendingUpgrades.TryGetValue(def, out var list))
            pendingUpgrades[def] = list = new List<PieceUpgradeSO>();

        list.Add(upg);
    }

    public List<PieceUpgradeSO> ConsumeUpgradesFor(PieceDefinition def)
    {
        if (def == null) return null;
        if (!pendingUpgrades.TryGetValue(def, out var list)) return null;
        return list;
    }

    public PieceDefinition CreateRuntimePiece(PieceDefinition template)
    {
        if (template == null) return null;

        var clone = ScriptableObject.Instantiate(template);
        clone.name = template.name + "_Runtime";
        return clone;
    }

    public int GetUpgradeCount(PieceDefinition def)
    {
        if (def == null) return 0;
        return _upgradeCounts.TryGetValue(def, out var used) ? used : 0;
    }

    public void IncrementUpgradeCount(PieceDefinition def)
    {
        if (def == null) return;

        _upgradeCounts.TryGetValue(def, out var used);
        _upgradeCounts[def] = used + 1;
    }

    public int GetUpgradeSlotsMax(PieceDefinition def)
    {
        if (def == null || def.piecePrefab == null) return 2;

        if (def.piecePrefab.TryGetComponent<PieceLoadout>(out var loadout))
            return Mathf.Max(1, loadout.defaultUpgradeSlots);

        return 2;
    }

    public void StartNewRun(ClanDefinition clan)
    {
        selectedClan = clan;
        startingTroopPool = (clan != null) ? clan.StartingTroopPool : null;

        army.Clear();
        runDeckNonLeaders.Clear(); // kept empty now unless you explicitly still want legacy deck support
        CurrentRunDeck.Clear();

        _upgradeCounts.Clear();
        pendingUpgrades.Clear();

        _queenDefRuntime = null;
        hasGrantedStartingTroop = false;

        isBossBattle = false;
        bossDefeated = false;
        selectedEncounter = null;

        MapState.ClearState();
        CurrencyManager.ClearSavedCurrency();
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.ResetCurrency();

        // Build leaders for prep
        if (selectedClan != null && selectedClan.queenDefinition != null)
        {
            var queenRuntime = CreateRuntimePiece(selectedClan.queenDefinition);
            _queenDefRuntime = queenRuntime;
            army.Add(queenRuntime);
        }
        else
        {
            Debug.LogError("[GameSession] selectedClan.queenDefinition not assigned.");
        }

        var troop = GrantRandomStartingTroop();

        // Build card-based combat deck from clan definition
        if (selectedClan != null && selectedClan.startingBattleDeck != null && selectedClan.startingBattleDeck.Length > 0)
        {
            CurrentRunDeck.AddRange(selectedClan.startingBattleDeck);
        }
        else
        {
            Debug.LogWarning("[GameSession] Clan has no startingBattleDeck assigned.");
        }

        Debug.Log($"[GameSession] Clan: {selectedClan?.clanName}");
        Debug.Log($"[GameSession] Queen leader: {_queenDefRuntime?.displayName}");
        Debug.Log($"[GameSession] Troop leader: {troop?.displayName}");
        Debug.Log($"[GameSession] Leaders (army) count: {army.Count}");
        Debug.Log($"[GameSession] CurrentRunDeck count: {CurrentRunDeck.Count}");
    }

    public PieceDefinition GrantRandomStartingTroop()
    {
        if (hasGrantedStartingTroop || startingTroopPool == null || startingTroopPool.Length == 0)
            return null;

        var pick = startingTroopPool[Random.Range(0, startingTroopPool.Length)];
        if (pick != null)
        {
            var runtime = CreateRuntimePiece(pick);
            army.Add(runtime);
            hasGrantedStartingTroop = true;
            return runtime;
        }

        return null;
    }

    public EncounterDefinition PickRandomEncounter()
    {
        if (encounterCatalog == null || encounterCatalog.encounters == null || encounterCatalog.encounters.Count == 0)
            return null;

        int i = Random.Range(0, encounterCatalog.encounters.Count);
        return encounterCatalog.encounters[i];
    }

    public IReadOnlyList<PieceDefinition> CurrentArmy => army;
}