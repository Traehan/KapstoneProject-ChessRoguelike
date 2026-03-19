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

    [Header("Card-Based Combat Deck (persistent for run)")]
    public List<CardDefinitionSO> CurrentRunDeck { get; private set; } = new();

    [Header("Spell Pool (Rewards given for RunDeck)")]
    public List<CardDefinitionSO> PotentialSpellPool;

    public bool hasGrantedStartingTroop = false;

    PieceDefinition _queenDefRuntime;

    readonly Dictionary<PieceDefinition, int> _upgradeCounts = new(); //TO DO: utilize the card generic prefab for upgrades for easier instantiation when spawned on the board
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

    public void StartNewRun(ClanDefinition clan)
    {
        selectedClan = clan;
        startingTroopPool = (clan != null) ? clan.StartingTroopPool : null;
        
        
        //Clears all previous GameSession Info If player chooses to reset
        army.Clear();
        CurrentRunDeck.Clear();
        PotentialSpellPool.Clear();

        _upgradeCounts.Clear();
        pendingUpgrades.Clear();

        _queenDefRuntime = null;
        hasGrantedStartingTroop = false;

        isBossBattle = false;
        bossDefeated = false;
        selectedEncounter = null;

        //Reset the map for a fresh new layout. TO DO: Change map logic when we introduce different node types (remove 2 cards node, duplicate card node, upgrade shop node, etc.)
        MapState.ClearState();
        // Currency manager is optimized for now, don't see a need to change anything with it just yet in terms of buying upgrades
        CurrencyManager.ClearSavedCurrency(); 
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.ResetCurrency();

        // Build leaders for prep, will ALWAYS add the clans queen at beginning of each run
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

        // Build card-based combat deck from clan definition with spellcardSOs given clanSO's default battledeck
        if (selectedClan != null && selectedClan.startingBattleDeck != null && selectedClan.startingBattleDeck.Length > 0)
        {
            CurrentRunDeck.AddRange(selectedClan.startingBattleDeck);
        }
        else
        {
            Debug.LogWarning("[GameSession] Clan has no startingBattleDeck assigned.");
        }
        
        //Adds ClanSO's list of all spells they can use, for the reward panel after each encounter win
        if (selectedClan != null && selectedClan.SpellPool != null && selectedClan.SpellPool.Length > 0)
        {
            PotentialSpellPool.AddRange(selectedClan.SpellPool);
        }
        else
        {
            Debug.LogWarning("[GameSession] Clan has no SpellPool assigned.");
        }
        
        
        //Just to check, can remove later
        Debug.Log($"[GameSession] Clan: {selectedClan?.clanName}");
        Debug.Log($"[GameSession] Queen leader: {_queenDefRuntime?.displayName}");
        Debug.Log($"[GameSession] Troop leader: {troop?.displayName}");
        Debug.Log($"[GameSession] Leaders (army) count: {army.Count}");
        Debug.Log($"[GameSession] CurrentRunDeck count: {CurrentRunDeck.Count}");
    }

    public PieceDefinition GrantRandomStartingTroop() // for pop up panel beginning of each run
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
    
    public EncounterDefinition PickRandomEncounter() //used for Map node logic
    {
        if (encounterCatalog == null || encounterCatalog.encounters == null || encounterCatalog.encounters.Count == 0)
            return null;

        int i = Random.Range(0, encounterCatalog.encounters.Count);
        return encounterCatalog.encounters[i];
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

    public IReadOnlyList<PieceDefinition> CurrentArmy => army;
}