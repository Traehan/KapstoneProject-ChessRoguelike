using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Chess;
using Card;

public class GameSession : MonoBehaviour
{
    public static GameSession I { get; private set; }

    [Header("Encounter Sources")]
    public EncounterCatalog encounterCatalog;

    [Header("Starting Troop Pool (map popup + recruit node)")]
    public PieceDefinition[] startingTroopPool;

    [Header("Unit Card Lookup (maps troop pieces to unit cards)")]
    public UnitCardDefinitionSO[] unitCardDefinitions;

    [Header("Run State (runtime)")]
    public ClanDefinition selectedClan;
    public EncounterDefinition selectedEncounter;
    public List<PieceDefinition> army = new();

    [Header("Card-Based Combat Deck (persistent for run)")]
    public List<CardDefinitionSO> CurrentRunDeck { get; private set; } = new();

    [Header("Spell Pool (Rewards given for RunDeck)")]
    public List<CardDefinitionSO> PotentialSpellPool;

    public bool hasGrantedStartingTroop = false;
    public bool hasShownStartingTroopPopup = false;

    PieceDefinition _queenDefRuntime;

    readonly Dictionary<PieceDefinition, int> _upgradeCounts = new();
    readonly Dictionary<PieceDefinition, List<PieceUpgradeSO>> pendingUpgrades = new();

    // NEW: lets us map a runtime army clone back to the original template piece
    readonly Dictionary<PieceDefinition, PieceDefinition> _runtimeToTemplate = new();

    [Header("Run / Boss State")]
    public bool isBossBattle;
    public bool bossDefeated;

    [Header("Chess Map Run State")]
    public int mapCurrentRow;
    public int mapCurrentColumn;
    public MapMovementType selectedMapMovementType = MapMovementType.Rook;

    [Header("Map Movement Counts")]
    public int rookMapMoveCount;
    public int bishopMapMoveCount;
    public int knightMapMoveCount;
    public int queenMapMoveCount;

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

        army.Clear();
        CurrentRunDeck.Clear();
        PotentialSpellPool.Clear();

        _upgradeCounts.Clear();
        pendingUpgrades.Clear();
        _runtimeToTemplate.Clear();

        _queenDefRuntime = null;
        hasGrantedStartingTroop = false;
        hasShownStartingTroopPopup = false;

        isBossBattle = false;
        bossDefeated = false;
        selectedEncounter = null;

        ResetMapMovementState();
        MapState.ClearState();

        CurrencyManager.ClearSavedCurrency();
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.ResetCurrency();

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

        if (selectedClan != null && selectedClan.startingBattleDeck != null && selectedClan.startingBattleDeck.Length > 0)
        {
            CurrentRunDeck.AddRange(selectedClan.startingBattleDeck);
        }
        else
        {
            Debug.LogWarning("[GameSession] Clan has no startingBattleDeck assigned.");
        }

        if (selectedClan != null && selectedClan.SpellPool != null && selectedClan.SpellPool.Length > 0)
        {
            PotentialSpellPool.AddRange(selectedClan.SpellPool);
        }
        else
        {
            Debug.LogWarning("[GameSession] Clan has no SpellPool assigned.");
        }

        Debug.Log($"[GameSession] Clan: {selectedClan?.clanName}");
        Debug.Log($"[GameSession] Queen leader: {_queenDefRuntime?.displayName}");
        Debug.Log($"[GameSession] Troop leader: {troop?.displayName}");
        Debug.Log($"[GameSession] Leaders (army) count: {army.Count}");
        Debug.Log($"[GameSession] CurrentRunDeck count: {CurrentRunDeck.Count}");
        Debug.Log($"[GameSession] Map start position: ({mapCurrentRow}, {mapCurrentColumn})");
        Debug.Log($"[GameSession] Move counts: R={rookMapMoveCount}, B={bishopMapMoveCount}, K={knightMapMoveCount}, Q={queenMapMoveCount}");
    }

    void ResetMapMovementState()
    {
        mapCurrentRow = 0;
        mapCurrentColumn = 2;

        selectedMapMovementType = MapMovementType.Rook;

        rookMapMoveCount = 99;
        bishopMapMoveCount = 99;
        knightMapMoveCount = 99;
        queenMapMoveCount = 0;
    }

    public int GetMapMovementCount(MapMovementType movementType)
    {
        switch (movementType)
        {
            case MapMovementType.Rook:
                return rookMapMoveCount;
            case MapMovementType.Bishop:
                return bishopMapMoveCount;
            case MapMovementType.Knight:
                return knightMapMoveCount;
            case MapMovementType.Queen:
                return queenMapMoveCount;
            default:
                return 0;
        }
    }

    public bool CanUseMapMovementType(MapMovementType movementType)
    {
        return GetMapMovementCount(movementType) > 0;
    }

    public void AddMapMovementCount(MapMovementType movementType, int amount)
    {
        if (amount <= 0) return;

        switch (movementType)
        {
            case MapMovementType.Rook:
                rookMapMoveCount += amount;
                break;
            case MapMovementType.Bishop:
                bishopMapMoveCount += amount;
                break;
            case MapMovementType.Knight:
                knightMapMoveCount += amount;
                break;
            case MapMovementType.Queen:
                queenMapMoveCount += amount;
                break;
        }
    }

    public bool TryConsumeMapMovement(MapMovementType movementType, int amount = 1)
    {
        if (amount <= 0) return true;

        int current = GetMapMovementCount(movementType);
        if (current < amount)
            return false;

        switch (movementType)
        {
            case MapMovementType.Rook:
                rookMapMoveCount -= amount;
                break;
            case MapMovementType.Bishop:
                bishopMapMoveCount -= amount;
                break;
            case MapMovementType.Knight:
                knightMapMoveCount -= amount;
                break;
            case MapMovementType.Queen:
                queenMapMoveCount -= amount;
                break;
        }

        return true;
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

    public List<UnitCardDefinitionSO> GetRecruitOptionsFromStartingTroopPool(int count)
    {
        var results = new List<UnitCardDefinitionSO>();

        if (startingTroopPool == null || startingTroopPool.Length == 0 || count <= 0)
            return results;

        List<PieceDefinition> pool = startingTroopPool
            .Where(x => x != null)
            .Distinct()
            .ToList();

        for (int i = 0; i < pool.Count; i++)
        {
            int rand = Random.Range(i, pool.Count);
            (pool[i], pool[rand]) = (pool[rand], pool[i]);
        }

        int take = Mathf.Min(count, pool.Count);

        for (int i = 0; i < take; i++)
        {
            UnitCardDefinitionSO unitCard = FindUnitCardForPiece(pool[i]);
            if (unitCard != null)
                results.Add(unitCard);
        }

        return results;
    }

    public UnitCardDefinitionSO FindUnitCardForPiece(PieceDefinition pieceDef)
    {
        if (pieceDef == null || unitCardDefinitions == null)
            return null;

        for (int i = 0; i < unitCardDefinitions.Length; i++)
        {
            var unitCard = unitCardDefinitions[i];
            if (unitCard == null) continue;

            if (unitCard.summonPieceDefinition == pieceDef)
                return unitCard;
        }

        Debug.LogWarning($"[GameSession] No UnitCardDefinitionSO found for piece: {pieceDef.name}");
        return null;
    }

    public void AddCardToRunDeck(CardDefinitionSO cardDef)
    {
        if (cardDef == null)
            return;

        CurrentRunDeck.Add(cardDef);
        Debug.Log($"[GameSession] Added card to CurrentRunDeck: {cardDef.GetDisplayName()}");
    }

    public bool RecruitUnit(UnitCardDefinitionSO unitCard)
    {
        if (unitCard == null)
            return false;

        var summonPiece = unitCard.summonPieceDefinition;
        if (summonPiece == null)
        {
            Debug.LogWarning("[GameSession] RecruitUnit failed: unit card has no summonPieceDefinition.");
            return false;
        }

        var runtimePiece = CreateRuntimePiece(summonPiece);
        if (runtimePiece == null)
        {
            Debug.LogWarning("[GameSession] RecruitUnit failed: could not create runtime piece.");
            return false;
        }

        army.Add(runtimePiece);

        Debug.Log($"[GameSession] Recruited army unit only: {runtimePiece.displayName}. Army count now {army.Count}.");
        return true;
    }

    public EncounterDefinition PickRandomEncounter()
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

    // NEW: safe read-only accessor for UI visuals
    public IReadOnlyList<PieceUpgradeSO> GetQueuedUpgradesFor(PieceDefinition def)
    {
        if (def == null) return null;
        return pendingUpgrades.TryGetValue(def, out var list) ? list : null;
    }

    public PieceDefinition CreateRuntimePiece(PieceDefinition template)
    {
        if (template == null) return null;

        var clone = ScriptableObject.Instantiate(template);
        clone.name = template.name + "_Runtime";

        _runtimeToTemplate[clone] = template;
        return clone;
    }

    // NEW: if passed a runtime piece, return the original template it was cloned from
    public PieceDefinition GetTemplateForRuntimePiece(PieceDefinition runtimePiece)
    {
        if (runtimePiece == null) return null;
        return _runtimeToTemplate.TryGetValue(runtimePiece, out var template) ? template : runtimePiece;
    }

    // NEW: find the army runtime piece that visually represents the given template/card piece
    public PieceDefinition ResolveArmyPieceForTemplate(PieceDefinition template)
    {
        if (template == null || army == null)
            return template;

        for (int i = 0; i < army.Count; i++)
        {
            var runtimePiece = army[i];
            if (runtimePiece == null)
                continue;

            if (runtimePiece == template)
                return runtimePiece;

            if (_runtimeToTemplate.TryGetValue(runtimePiece, out var sourceTemplate) && sourceTemplate == template)
                return runtimePiece;
        }

        return template;
    }

    // NEW: convenience for card UI
    public PieceDefinition ResolveDisplayPieceForCard(CardDefinitionSO cardDef)
    {
        if (cardDef is UnitCardDefinitionSO unitDef && unitDef.summonPieceDefinition != null)
            return ResolveArmyPieceForTemplate(unitDef.summonPieceDefinition);

        return null;
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
    
    public bool DevSwapClanMidRun(ClanDefinition newClan, bool grantRandomStartingTroop = true)
{
    if (newClan == null)
    {
        Debug.LogWarning("[GameSession] DevSwapClanMidRun failed: newClan is null.");
        return false;
    }

    selectedClan = newClan;
    startingTroopPool = newClan.StartingTroopPool;

    // Replace clan-owned run content, but DO NOT reset map progress/currency.
    army.Clear();
    CurrentRunDeck.Clear();
    PotentialSpellPool.Clear();

    _upgradeCounts.Clear();
    pendingUpgrades.Clear();

    _queenDefRuntime = null;
    hasGrantedStartingTroop = false;

    // Since this is a dev swap on the map, prevent the opening popup from reappearing.
    hasShownStartingTroopPopup = true;

    // Make sure no old encounter is still queued.
    selectedEncounter = null;
    isBossBattle = false;

    if (selectedClan.queenDefinition != null)
    {
        var queenRuntime = CreateRuntimePiece(selectedClan.queenDefinition);
        _queenDefRuntime = queenRuntime;
        army.Add(queenRuntime);
    }
    else
    {
        Debug.LogError("[GameSession] DevSwapClanMidRun: selectedClan.queenDefinition not assigned.");
    }

    if (grantRandomStartingTroop)
    {
        var troop = GrantRandomStartingTroop();
        Debug.Log($"[GameSession] Dev clan swap troop: {troop?.displayName}");
    }

    if (selectedClan.startingBattleDeck != null && selectedClan.startingBattleDeck.Length > 0)
        CurrentRunDeck.AddRange(selectedClan.startingBattleDeck);
    else
        Debug.LogWarning("[GameSession] DevSwapClanMidRun: clan has no startingBattleDeck assigned.");

    if (selectedClan.SpellPool != null && selectedClan.SpellPool.Length > 0)
        PotentialSpellPool.AddRange(selectedClan.SpellPool);
    else
        Debug.LogWarning("[GameSession] DevSwapClanMidRun: clan has no SpellPool assigned.");

    Debug.Log(
        $"[GameSession] DEV CLAN SWAP -> {selectedClan.clanName} | " +
        $"Army={army.Count}, RunDeck={CurrentRunDeck.Count}, SpellPool={PotentialSpellPool.Count}"
    );

    return true;
}

    public IReadOnlyList<PieceDefinition> CurrentArmy => army;
}