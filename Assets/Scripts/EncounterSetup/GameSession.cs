// Assets/Scripts/_Core/GameSession.cs
using System.Collections.Generic;
using UnityEngine;
using Chess;

public class GameSession : MonoBehaviour
{
    public static GameSession I { get; private set; }

    [Header("Encounter Sources")]
    public EncounterCatalog encounterCatalog;       // assign in inspector

    [Header("Starting Troop Pool (map popup)")]
    public PieceDefinition[] startingTroopPool;     // assign Bishop/Knight/Rook defs

    [Header("Run State (runtime)")]
    public ClanDefinition selectedClan;             // picked on Clan Select scene
    public EncounterDefinition selectedEncounter;   // used by map → battle
    public List<PieceDefinition> army = new();      // runtime “army” (NOT icons on board)

    [Header("Queen Icon (optional but recommended)")]
    public Sprite queenIconFallback;                  // assign a queen sprite in the inspector
    public GameObject queenIconPrefabOverride;        // optional: per-queen icon prefab
    
    [Header("Combat Deck (non-leaders, persistent for run)")]
    public PieceDefinition pawnTemplate;                 // assign Pawn_Def in inspector
    public List<PieceDefinition> runDeckNonLeaders = new(); // Pawn x8 + shop adds
    
    // One-time grant flag per run
    public bool hasGrantedStartingTroop = false;

    // Runtime queen wrapper (we create a PieceDefinition from the clan’s queen prefab)
    PieceDefinition _queenDefRuntime;

    // Tracks how many slots are used per runtime definition
    readonly Dictionary<PieceDefinition, int> _upgradeCounts = new();

    // queue of upgrades to apply when a piece instance spawns
    readonly Dictionary<PieceDefinition, List<PieceUpgradeSO>> pendingUpgrades
        = new();
    
    [Header("Run / Boss State")]
    public bool isBossBattle;
    public bool bossDefeated;


    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    // --- Upgrade queue API ---
    public void QueueUpgrade(PieceDefinition def, PieceUpgradeSO upg)
    {
        if (def == null || upg == null) return;
        if (!pendingUpgrades.TryGetValue(def, out var list))
            pendingUpgrades[def] = list = new List<PieceUpgradeSO>();
        list.Add(upg);
    }

    public List<PieceUpgradeSO> ConsumeUpgradesFor(PieceDefinition def)
    {
        // Note: now this is effectively "GetUpgradesFor".
        // We *do not* remove them, so they persist for the whole run.
        if (def == null) return null;
        if (!pendingUpgrades.TryGetValue(def, out var list)) return null;
        return list;
    }


    // ==== Run lifecycle ====

    // Make a unique, per-run copy of a PieceDefinition (shop purchases, start troop)
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
        // Read max slots from the piece prefab's PieceLoadout
        if (def == null || def.piecePrefab == null) return 2;

        if (def.piecePrefab.TryGetComponent<PieceLoadout>(out var loadout))
            return Mathf.Max(1, loadout.defaultUpgradeSlots);

        return 2;
    }

    /// Call when the player picks a clan on the Clan Select scene.
    public void StartNewRun(ClanDefinition clan)
    {
        // --- persist clan + pools ---
        selectedClan = clan;
        startingTroopPool = (clan != null) ? clan.StartingTroopPool : null;

        // --- reset run state ---
        army.Clear();                 // leaders for prep: queen + troop
        runDeckNonLeaders.Clear();    // combat deck: pawns + shop adds

        _upgradeCounts.Clear();
        pendingUpgrades.Clear();

        _queenDefRuntime = null;
        hasGrantedStartingTroop = false;

        MapState.ClearState();
        CurrencyManager.ClearSavedCurrency();
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.ResetCurrency();

        // --- build leaders (prep only) ---
        EnsureQueenPieceDefinition();
        if (_queenDefRuntime != null && !army.Contains(_queenDefRuntime))
            army.Add(_queenDefRuntime);

        var troop = GrantRandomStartingTroop(); // adds to army + sets hasGrantedStartingTroop

        // --- build combat deck (non-leaders) ---
        if (pawnTemplate == null)
        {
            Debug.LogError("[GameSession] pawnTemplate is not assigned.");
        }
        else
        {
            for (int i = 0; i < 8; i++)
            {
                var pawnRuntime = CreateRuntimePiece(pawnTemplate);
                runDeckNonLeaders.Add(pawnRuntime);
            }
        }

        // --- logs (milestone proof) ---
        Debug.Log($"[GameSession] Clan: {selectedClan?.clanName}");
        Debug.Log($"[GameSession] Queen leader: {_queenDefRuntime?.displayName}");
        Debug.Log($"[GameSession] Troop leader: {troop?.displayName}");
        Debug.Log($"[GameSession] Leaders (army) count: {army.Count}");
        Debug.Log($"[GameSession] NonLeaderDeck count: {runDeckNonLeaders.Count} (expected 8)");
    }

    /// Called by Map scene once (on first open) to grant a random starting troop.
    public PieceDefinition GrantRandomStartingTroop()
    {
        if (hasGrantedStartingTroop || startingTroopPool == null || startingTroopPool.Length == 0)
            return null;

        var pick = startingTroopPool[Random.Range(0, startingTroopPool.Length)];
        if (pick != null)
        {
            var runtime = CreateRuntimePiece(pick);   // unique copy
            army.Add(runtime);
            hasGrantedStartingTroop = true;
            return runtime;
        }
        return null;
    }

    /// Your map uses this when a node is clicked.
    public EncounterDefinition PickRandomEncounter()
    {
        if (encounterCatalog == null || encounterCatalog.encounters == null || encounterCatalog.encounters.Count == 0)
            return null;
        int i = Random.Range(0, encounterCatalog.encounters.Count);
        return encounterCatalog.encounters[i];
    }

    /// The PrepPanel needs the current army list (queen + granted troops).
    public IReadOnlyList<PieceDefinition> CurrentArmy => army;

    // ==== Helpers ====

    void EnsureQueenPieceDefinition()
    {
        if (_queenDefRuntime != null) return;
        if (selectedClan == null || selectedClan.queenPrefab == null) return;

        _queenDefRuntime = ScriptableObject.CreateInstance<PieceDefinition>();
        _queenDefRuntime.displayName = $"{selectedClan.clanName} Queen";
        _queenDefRuntime.piecePrefab = selectedClan.queenPrefab;
        _queenDefRuntime.count = 1;

        // visuals for PrepPanel icon
        _queenDefRuntime.icon = queenIconFallback;
        _queenDefRuntime.iconPrefabOverride = queenIconPrefabOverride;
    }

    private void ClearStartingTroopPool(PieceDefinition[] Pool)
    {
        //Clears starting troop pool
    }

    private void FillStartingTroopPool(PieceDefinition[] ClanPool, PieceDefinition[] StartingTroopPool)
    {
        //fills starting troop pool with ClanPool definitions
    }

    private void AssignArmyGivenClan(ClanDefinition clan, PieceDefinition[] Pool, PieceDefinition[] StartingTroopPool)
    {
        //on each clan button click, if another clanArmyPool is present within the starting troop pool, clears it and fills it with other clan pool
    }
}
