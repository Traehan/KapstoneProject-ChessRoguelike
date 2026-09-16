using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Chess;
using GameManager;
using Random = UnityEngine.Random;

public class MapGenerator : MonoBehaviour
{
    [Header("Board Settings")]
    [Tooltip("Columns in the chess-style map.")]
    public int boardWidth = 5;

    [Tooltip("Number of normal playable rows between Start and Boss.")]
    public int playableRows = 8;

    [Header("Visual Settings (3D)")]
    [Tooltip("MapTile3D prefab instantiated per visible node.")]
    public MapTile3D mapTilePrefab;
    [Tooltip("Plain Transform tiles are parented under. World-space X = columns, Z = rows.")]
    public Transform mapTilesRoot;
    [Tooltip("Tile is 4 units wide (see MapTile3D.prefab) - keep this bigger than 4 to leave a visible gap between tiles for decoration/path art.")]
    public float tileSpacingX = 8f;
    [Tooltip("Also drives how many rows are visible by default before free-look is needed (see MapCameraRig) - changing this without retuning the camera's height/tilt changes how many rows show.")]
    public float tileSpacingZ = 10f;

    [Header("Camera / Token (3D)")]
    public MapClanToken mapClanToken;
    public MapCameraRig mapCameraRig;

    [Header("Scene References")]
    public string battleSceneName = "SampleScene";
    public string shopSceneName = "ShopScene";
    public string eventSceneName = "EventScene";
    [Tooltip("Seconds to hold on the map after picking an Encounter/Shop/Boss node before actually loading the scene, so the clan token's move + camera follow are visible first. Doesn't apply to same-scene popups (Recruit/RemoveTwoCards/DuplicateCard).")]
    public float nodeSelectTransitionDelay = 1.5f;

    [Header("Boss Settings")]
    public EncounterDefinition bossEncounter;

    [Header("Gauntlet (Elite/Trial) Settings")]
    [Tooltip("Minimum number of a row's Encounter nodes flagged as Gauntlets (only applies if that row has at least this many Encounter nodes).")]
    [Min(0)] public int minGauntletPerRow = 1;
    [Tooltip("Maximum number of a row's Encounter nodes flagged as Gauntlets. Dial down to 1 for a future ascension-style difficulty option.")]
    [Min(0)] public int maxGauntletPerRow = 2;

    [Header("Run Complete UI")]
    [SerializeField] GameObject runCompletePanel;
    [SerializeField] Button restartRunButton;
    [SerializeField] Button quitGameButton;
    [SerializeField] string clanSelectSceneName = "ClanSelectScene";
    [Header("Node Panels")]
    [SerializeField] RecruitNodePanel recruitNodePanel;

    [Header("Deck Editing (Remove Two / Duplicate Card node events)")]
    [Tooltip("Leave empty to use DeckViewController.Instance (fine - it's the persistent-scene DeckViewController, " +
             "loaded before the player can ever reach the map). Only assign directly if MapScene ever gets its own " +
             "local DeckViewController and needs to bypass the persistent one.")]
    [SerializeField] DeckViewController deckViewController;

    DeckViewController ActiveDeckViewController => deckViewController != null ? deckViewController : DeckViewController.Instance;

    readonly List<List<MapNode>> mapRows = new();
    readonly Dictionary<MapNode, MapTile3D> nodeVisuals = new();

    // start row + playable rows + boss row
    int TotalRows => playableRows + 2;
    int CenterColumn => Mathf.Max(0, boardWidth / 2);
    int BossRowIndex => TotalRows - 1;

    GameSession GS => GameSession.I;

    void OnEnable()
    {
        Debug.Log($"[MapGenerator] OnEnable in scene {SceneManager.GetActiveScene().name}");

        LoadOrGenerateMap();
        // StartCoroutine(CenterScrollOnCurrentNodeAfterLayout());

        if (runCompletePanel != null)
            runCompletePanel.SetActive(false);

        if (restartRunButton != null)
        {
            restartRunButton.onClick.RemoveListener(OnRestartRunClicked);
            restartRunButton.onClick.AddListener(OnRestartRunClicked);
        }

        if (quitGameButton != null)
        {
            quitGameButton.onClick.RemoveListener(OnQuitGameClicked);
            quitGameButton.onClick.AddListener(OnQuitGameClicked);
        }

        CheckIfRunIsCompletePanel();
    }

    void OnDisable()
    {
        if (restartRunButton != null)
            restartRunButton.onClick.RemoveListener(OnRestartRunClicked);

        if (quitGameButton != null)
            quitGameButton.onClick.RemoveListener(OnQuitGameClicked);
    }

    void LoadOrGenerateMap()
    {
        MapState savedState = MapState.LoadState();

        if (savedState != null &&
            savedState.isValid &&
            savedState.rows != null &&
            savedState.rows.Count > 0)
        {
            Debug.Log("[MapGenerator] Restoring map from saved chessboard state.");
            RestoreMapFromState(savedState);
        }
        else
        {
            Debug.Log("[MapGenerator] No valid saved chessboard state. Generating new map.");
            GenerateNewMap();
        }
    }

    private int GenerateRandomIntMapMovementTypes()
    {
        return Random.Range(0, 2);
    }

    void GenerateNewMap()
    {
        mapRows.Clear();
        ClearVisuals();

        if (GS != null)
        {
            GS.mapCurrentRow = 0;
            GS.mapCurrentColumn = CenterColumn;
            GS.selectedMapMovementType = MapMovementType.Rook;

            GS.rookMapMoveCount = 0;
            GS.bishopMapMoveCount = 0;
            GS.knightMapMoveCount = 0;
            GS.queenMapMoveCount = 0;

            GS.GrantRandomDifferentMapMovements(2, includeQueen: false);
            GS.selectedMapMovementType = GetFirstAvailableMovementType();
        }

        for (int row = 0; row < TotalRows; row++)
        {
            List<MapNode> rowNodes = new List<MapNode>();

            for (int col = 0; col < boardWidth; col++)
            {
                MapNode node = BuildNode(row, col);
                rowNodes.Add(node);
            }

            FlagGauntletNodesForRow(rowNodes);

            mapRows.Add(rowNodes);
        }

        var startNode = GetNodeAt(0, CenterColumn);
        if (startNode != null)
        {
            startNode.SetAsStartTile();
            startNode.isVisited = true;
            startNode.isCurrentlyAvailable = false;
        }

        RefreshAvailableNodes();
        CreateVisuals();
        SaveMapState();
        SnapTokenAndCameraToCurrent();
    }

    /// <summary>World position for a given (row, column) grid cell, matching CreateVisuals' placement.</summary>
    Vector3 GetNodeWorldPosition(int row, int column)
    {
        float x = (column - (boardWidth - 1) * 0.5f) * tileSpacingX;
        float z = row * tileSpacingZ;
        Vector3 origin = mapTilesRoot != null ? mapTilesRoot.position : Vector3.zero;
        return origin + new Vector3(x, 0f, z);
    }

    /// <summary>Instantly places the clan token and camera on the player's current node - no animation.
    /// Used on scene load/restore/dev-jump instead of the old scroll-centering coroutine.</summary>
    void SnapTokenAndCameraToCurrent()
    {
        Vector3 pos = GetNodeWorldPosition(GetCurrentRow(), GetCurrentColumn());

        if (mapClanToken != null)
            mapClanToken.SnapTo(pos);

        if (mapCameraRig != null)
        {
            mapCameraRig.ConfigureBounds(boardWidth, TotalRows, tileSpacingX, tileSpacingZ,
                mapTilesRoot != null ? mapTilesRoot.position : Vector3.zero);
            mapCameraRig.SnapToToken();
        }
    }

    MapNode BuildNode(int row, int col)
    {
        // Start row
        if (row == 0)
        {
            if (col == CenterColumn)
                return new MapNode(row, col, MapNodeType.Start);

            var hidden = new MapNode(row, col, MapNodeType.Hidden);
            hidden.isVisited = true;
            hidden.isCurrentlyAvailable = false;
            return hidden;
        }

        // Boss row
        if (row == BossRowIndex)
        {
            if (col == CenterColumn)
            {
                var bossNode = new MapNode(row, col, MapNodeType.Boss);
                bossNode.SetAsBossTile();
                bossNode.encounter = bossEncounter != null ? bossEncounter : GetRandomEncounter();
                return bossNode;
            }

            var hidden = new MapNode(row, col, MapNodeType.Hidden);
            hidden.isVisited = true;
            hidden.isCurrentlyAvailable = false;
            return hidden;
        }

        MapNodeType type = GetNodeTypeForRow(row);
        MapNode node = new MapNode(row, col, type);

        if (type == MapNodeType.Encounter)
            node.encounter = GetRandomEncounter();

        return node;
    }

    MapNodeType GetNodeTypeForRow(int row) //TWEAK TO FIX MAP RANDOMNESS
    {
        if (row <= 0)
            return MapNodeType.Start;

        if (row % 2 == 1)
        {
            int roll = Random.Range(0, 100);
            if (roll < 80) return MapNodeType.Encounter;
            if (roll < 90) return MapNodeType.Shop;
            return MapNodeType.Recruit;
        }

        int evenRoll = Random.Range(0, 100);

        if (evenRoll < 30) return MapNodeType.Encounter;
        if (evenRoll < 50) return MapNodeType.Shop;
        if (evenRoll < 65) return MapNodeType.Recruit;
        if (evenRoll < 82) return MapNodeType.RemoveTwoCards;
        return MapNodeType.DuplicateCard;
    }

    // Post-process step for a freshly-built row: flags between minGauntletPerRow and maxGauntletPerRow
    // of that row's Encounter nodes as Gauntlets (no-ops for rows with no Encounter nodes, e.g. Start/
    // Boss rows), rolling independent random reward/challenge types per flagged node.
    void FlagGauntletNodesForRow(List<MapNode> rowNodes)
    {
        List<MapNode> encounterNodes = rowNodes.Where(n => n != null && n.nodeType == MapNodeType.Encounter).ToList();
        if (encounterNodes.Count == 0)
            return;

        // Fisher-Yates shuffle so which nodes get flagged is unbiased by column order.
        for (int i = 0; i < encounterNodes.Count; i++)
        {
            int rand = Random.Range(i, encounterNodes.Count);
            (encounterNodes[i], encounterNodes[rand]) = (encounterNodes[rand], encounterNodes[i]);
        }

        int minCount = Mathf.Clamp(minGauntletPerRow, 0, encounterNodes.Count);
        int maxCount = Mathf.Clamp(Mathf.Max(maxGauntletPerRow, minCount), minCount, encounterNodes.Count);
        int count = Random.Range(minCount, maxCount + 1);

        int rewardTypeCount = System.Enum.GetValues(typeof(GauntletRewardType)).Length;
        int challengeTypeCount = System.Enum.GetValues(typeof(GauntletChallengeType)).Length;

        for (int i = 0; i < count; i++)
        {
            MapNode node = encounterNodes[i];
            node.isGauntlet = true;
            node.gauntletReward = (GauntletRewardType)Random.Range(0, rewardTypeCount);
            node.gauntletChallenge = (GauntletChallengeType)Random.Range(0, challengeTypeCount);
        }
    }

    void CreateVisuals()
    {
        if (mapTilePrefab == null || mapTilesRoot == null)
        {
            Debug.LogError("[MapGenerator] Missing mapTilePrefab or mapTilesRoot.");
            return;
        }

        ClearVisuals();

        for (int row = 0; row < mapRows.Count; row++)
        {
            for (int col = 0; col < mapRows[row].Count; col++)
            {
                MapNode node = mapRows[row][col];
                if (node == null) continue;
                if (node.nodeType == MapNodeType.Hidden) continue;

                Vector3 worldPos = GetNodeWorldPosition(row, col);

                MapTile3D tile = Instantiate(mapTilePrefab, worldPos, Quaternion.identity, mapTilesRoot);
                tile.Initialize(node, this);
                nodeVisuals[node] = tile;
            }
        }

        UpdateAllVisuals();
    }

    void ClearVisuals()
    {
        foreach (var kvp in nodeVisuals)
        {
            if (kvp.Value != null && kvp.Value.gameObject != null)
                Destroy(kvp.Value.gameObject);
        }

        nodeVisuals.Clear();
    }

    public void OnNodeSelected(MapNode selectedNode)
    {
        if (selectedNode == null) return;
        if (!selectedNode.isCurrentlyAvailable) return;
        if (GS == null) return;

        MapMovementType movementUsed = GetSelectedMovementType();

        if (!CanUseMovement(movementUsed))
        {
            movementUsed = GetFirstAvailableMovementType();

            if (movementUsed == MapMovementType.None)
            {
                Debug.LogWarning("[MapGenerator] No available map movement types left.");
                RefreshAvailableNodes();
                UpdateAllVisuals();
                SaveMapState();
                return;
            }

            GS.selectedMapMovementType = movementUsed;
        }

        if (!GS.TryConsumeMapMovement(movementUsed, 1))
        {
            Debug.LogWarning($"[MapGenerator] Failed to consume movement: {movementUsed}");
            RefreshAvailableNodes();
            UpdateAllVisuals();
            SaveMapState();
            return;
        }

        Debug.Log($"[MapGenerator] Consumed 1 {movementUsed} movement.");

        GS.mapCurrentRow = selectedNode.row;
        GS.mapCurrentColumn = selectedNode.column;

        selectedNode.Visit();

        RefreshAvailableNodes();
        UpdateAllVisuals();
        SaveMapState();

        if (mapClanToken != null)
            mapClanToken.MoveTo(GetNodeWorldPosition(selectedNode.row, selectedNode.column));

        mapCameraRig?.NotifyNodeSelected();

        StartCoroutine(NavigateToNodeScene(selectedNode));
    }

    void RefreshAvailableNodes()
    {
        for (int row = 0; row < mapRows.Count; row++)
        {
            for (int col = 0; col < mapRows[row].Count; col++)
            {
                var node = mapRows[row][col];
                if (node == null) continue;
                node.SetAvailable(false);
            }
        }

        int currentRow = GetCurrentRow();

        if (currentRow >= BossRowIndex - 1)
        {
            var bossNode = GetNodeAt(BossRowIndex, CenterColumn);
            if (bossNode != null && !bossNode.isVisited)
                bossNode.SetAvailable(true);

            return;
        }

        MapMovementType selectedMove = GetSelectedMovementType();
        if (!CanUseMovement(selectedMove))
        {
            selectedMove = GetFirstAvailableMovementType();
            if (GS != null)
                GS.selectedMapMovementType = selectedMove;
        }

        if (selectedMove == MapMovementType.None)
            return;

        List<Vector2Int> candidates = GetCandidateMoves(selectedMove);

        for (int i = 0; i < candidates.Count; i++)
        {
            Vector2Int coord = candidates[i];

            if (!IsCoordValid(coord.x, coord.y))
                continue;

            MapNode node = GetNodeAt(coord.x, coord.y);
            if (node == null) continue;
            if (node.isVisited) continue;
            if (node.nodeType == MapNodeType.Hidden) continue;

            node.SetAvailable(true);
        }
    }

    List<Vector2Int> GetCandidateMoves(MapMovementType moveType)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        int r = GetCurrentRow();
        int c = GetCurrentColumn();

        switch (moveType)
        {
            case MapMovementType.Rook:
                moves.Add(new Vector2Int(r + 1, c));
                break;

            case MapMovementType.Bishop:
                moves.Add(new Vector2Int(r + 1, c - 1));
                moves.Add(new Vector2Int(r + 1, c + 1));
                break;

            case MapMovementType.Knight:
                moves.Add(new Vector2Int(r + 1, c - 2));
                moves.Add(new Vector2Int(r + 1, c + 2));
                break;

            case MapMovementType.Queen:
                moves.Add(new Vector2Int(r + 1, c));
                moves.Add(new Vector2Int(r + 1, c - 1));
                moves.Add(new Vector2Int(r + 1, c + 1));
                moves.Add(new Vector2Int(r, c - 1));
                moves.Add(new Vector2Int(r, c + 1));
                break;
        }

        return moves;
    }

    bool IsCoordValid(int row, int column)
    {
        if (row < 0 || row >= mapRows.Count) return false;
        if (column < 0 || column >= boardWidth) return false;
        return true;
    }

    MapNode GetNodeAt(int row, int column)
    {
        if (!IsCoordValid(row, column))
            return null;

        return mapRows[row][column];
    }

    public bool IsPlayerOnNode(MapNode node)
    {
        if (node == null || GS == null) return false;
        return node.row == GS.mapCurrentRow && node.column == GS.mapCurrentColumn;
    }

    public void SelectMovementType(MapMovementType movementType)
    {
        if (GS == null) return;

        if (!CanUseMovement(movementType))
        {
            Debug.LogWarning($"[MapGenerator] Tried to select movement with 0 count: {movementType}");
            return;
        }

        Debug.Log($"[MapGenerator] Selected movement type: {movementType}");

        GS.selectedMapMovementType = movementType;
        RefreshAvailableNodes();
        UpdateAllVisuals();
        SaveMapState();
    }

    public void SelectRookMovement() => SelectMovementType(MapMovementType.Rook);
    public void SelectBishopMovement() => SelectMovementType(MapMovementType.Bishop);
    public void SelectKnightMovement() => SelectMovementType(MapMovementType.Knight);
    public void SelectQueenMovement() => SelectMovementType(MapMovementType.Queen);

    bool CanUseMovement(MapMovementType movementType)
    {
        if (GS == null) return false;
        return GS.CanUseMapMovementType(movementType);
    }

    MapMovementType GetFirstAvailableMovementType()
    {
        if (GS == null) return MapMovementType.Rook;

        if (GS.rookMapMoveCount > 0) return MapMovementType.Rook;
        if (GS.bishopMapMoveCount > 0) return MapMovementType.Bishop;
        if (GS.knightMapMoveCount > 0) return MapMovementType.Knight;
        if (GS.queenMapMoveCount > 0) return MapMovementType.Queen;

        return MapMovementType.None;
    }

    int GetCurrentRow()
    {
        return GS != null ? GS.mapCurrentRow : 0;
    }

    int GetCurrentColumn()
    {
        return GS != null ? GS.mapCurrentColumn : CenterColumn;
    }

    MapMovementType GetSelectedMovementType()
    {
        return GS != null ? GS.selectedMapMovementType : MapMovementType.Rook;
    }

    IEnumerator NavigateToNodeScene(MapNode node)
    {
        // Do not enter a separate scene for Start
        if (node.nodeType == MapNodeType.Start)
            yield break;

        var gs = GameSession.I;
        if (gs == null)
        {
            Debug.LogError("[MapGenerator] GameSession missing in MapScene.");
            yield break;
        }

        // Stash/clear the Gauntlet flag for the next scene before anything else - a normal fight
        // (or any non-Encounter node) must never accidentally show the Gauntlet screen with stale data.
        gs.pendingGauntletAvailable = node.nodeType == MapNodeType.Encounter && node.isGauntlet;
        if (gs.pendingGauntletAvailable)
        {
            gs.pendingGauntletReward = node.gauntletReward;
            gs.pendingGauntletChallenge = node.gauntletChallenge;
        }
        gs.gauntletAccepted = false;

        string targetScene = battleSceneName;
        object sceneArgs = null;

        switch (node.nodeType)
        {
            case MapNodeType.Encounter:
                targetScene = battleSceneName;

                if (node.encounter != null)
                {
                    gs.selectedEncounter = node.encounter;
                    sceneArgs = node.encounter;
                }
                else
                {
                    var randomEncounter = gs.PickRandomEncounter();
                    if (randomEncounter == null)
                    {
                        Debug.LogError("[MapGenerator] No encounter available.");
                        yield break;
                    }

                    gs.selectedEncounter = randomEncounter;
                    sceneArgs = randomEncounter;
                }
                break;

            case MapNodeType.Shop:
                targetScene = shopSceneName;
                break;

            case MapNodeType.Recruit:
                if (recruitNodePanel == null)
                {
                    Debug.LogWarning("[MapGenerator] No RecruitNodePanel assigned for Recruit node.");
                    yield break;
                }

                recruitNodePanel.Open();
                yield break;

            case MapNodeType.Boss:
                targetScene = battleSceneName;
                gs.isBossBattle = true;
                gs.bossDefeated = false;

                if (bossEncounter != null)
                {
                    gs.selectedEncounter = bossEncounter;
                    sceneArgs = bossEncounter;
                }
                else if (node.encounter != null)
                {
                    gs.selectedEncounter = node.encounter;
                    sceneArgs = node.encounter;
                }
                else
                {
                    var randomBossFallback = gs.PickRandomEncounter();
                    if (randomBossFallback == null)
                    {
                        Debug.LogError("[MapGenerator] No boss encounter available.");
                        yield break;
                    }

                    gs.selectedEncounter = randomBossFallback;
                    sceneArgs = randomBossFallback;
                }
                break;
            
            case MapNodeType.RemoveTwoCards:
            {
                var ctrl = ActiveDeckViewController;
                if (ctrl == null)
                {
                    Debug.LogWarning("[MapGenerator] No DeckViewController available for RemoveTwoCards event.");
                    yield break;
                }

                ctrl.OpenRemoveTwoMode();
                yield break;
            }

            case MapNodeType.DuplicateCard:
            {
                var ctrl = ActiveDeckViewController;
                if (ctrl == null)
                {
                    Debug.LogWarning("[MapGenerator] No DeckViewController available for DuplicateCard event.");
                    yield break;
                }

                ctrl.OpenDuplicateOneMode();
                yield break;
            }
        }

        // Give the clan token/camera time to visibly finish moving to the new node before cutting away.
        yield return new WaitForSeconds(nodeSelectTransitionDelay);

        yield return SceneController.instance.GoTo(targetScene, sceneArgs);
    }

    void SaveMapState()
    {
        MapState state = new MapState
        {
            isValid = true,
            boardWidth = this.boardWidth,
            playableRows = this.playableRows,
            totalRows = this.TotalRows,
            currentPlayerRow = GetCurrentRow(),
            currentPlayerColumn = GetCurrentColumn(),
            selectedMovementType = GetSelectedMovementType(),
            movementInventory = new MapMovementInventoryData
            {
                rookCount = GS != null ? GS.rookMapMoveCount : 0,
                bishopCount = GS != null ? GS.bishopMapMoveCount : 0,
                knightCount = GS != null ? GS.knightMapMoveCount : 0,
                queenCount = GS != null ? GS.queenMapMoveCount : 0
            },
            rows = new List<MapRowData>()
        };

        for (int row = 0; row < mapRows.Count; row++)
        {
            MapRowData rowData = new MapRowData();

            for (int col = 0; col < mapRows[row].Count; col++)
            {
                MapNode node = mapRows[row][col];
                if (node == null) continue;

                MapNodeData nodeData = new MapNodeData
                {
                    row = node.row,
                    column = node.column,
                    nodeType = node.nodeType,
                    isVisited = node.isVisited,
                    isCurrentlyAvailable = node.isCurrentlyAvailable,
                    isStartTile = node.isStartTile,
                    isBossTile = node.isBossTile,
                    isGauntlet = node.isGauntlet,
                    gauntletReward = node.gauntletReward,
                    gauntletChallenge = node.gauntletChallenge
                };

                rowData.nodes.Add(nodeData);
            }

            state.rows.Add(rowData);
        }

        MapState.SaveState(state);
    }

    void RestoreMapFromState(MapState state)
    {
        mapRows.Clear();
        ClearVisuals();

        boardWidth = Mathf.Max(1, state.boardWidth);
        playableRows = Mathf.Max(1, state.playableRows);

        if (GS != null)
        {
            GS.mapCurrentRow = state.currentPlayerRow;
            GS.mapCurrentColumn = state.currentPlayerColumn;

            if (GS.CanUseMapMovementType(GS.selectedMapMovementType))
            {
                // Keep current selected movement if it is still usable.
            }
            else if (state.selectedMovementType != MapMovementType.None && GS.CanUseMapMovementType(state.selectedMovementType))
            {
                GS.selectedMapMovementType = state.selectedMovementType;
            }
            else
            {
                GS.selectedMapMovementType = GetFirstAvailableMovementType();
            }

            Debug.Log($"[MapGenerator] Restored map position, keeping GameSession movement counts: R={GS.rookMapMoveCount}, B={GS.bishopMapMoveCount}, K={GS.knightMapMoveCount}, Q={GS.queenMapMoveCount}");
        }

        for (int row = 0; row < state.rows.Count; row++)
        {
            MapRowData rowData = state.rows[row];
            List<MapNode> rebuiltRow = new List<MapNode>();

            for (int i = 0; i < rowData.nodes.Count; i++)
            {
                MapNodeData savedNode = rowData.nodes[i];
                MapNode node = new MapNode(savedNode.row, savedNode.column, savedNode.nodeType)
                {
                    isVisited = savedNode.isVisited,
                    isCurrentlyAvailable = savedNode.isCurrentlyAvailable,
                    isStartTile = savedNode.isStartTile,
                    isBossTile = savedNode.isBossTile,
                    isGauntlet = savedNode.isGauntlet,
                    gauntletReward = savedNode.gauntletReward,
                    gauntletChallenge = savedNode.gauntletChallenge
                };

                if (node.nodeType == MapNodeType.Encounter)
                    node.encounter = GetRandomEncounter();
                else if (node.nodeType == MapNodeType.Boss)
                    node.encounter = bossEncounter != null ? bossEncounter : GetRandomEncounter();

                rebuiltRow.Add(node);
            }

            mapRows.Add(rebuiltRow);
        }

        RefreshAvailableNodes();
        CreateVisuals();
        UpdateAllVisuals();
        SnapTokenAndCameraToCurrent();
    }

    void UpdateAllVisuals()
    {
        foreach (var kvp in nodeVisuals)
        {
            if (kvp.Value != null)
                kvp.Value.UpdateVisuals();
        }
    }

    public void ResetMap()
    {
        MapState.ClearState();
        GenerateNewMap();
    }

    EncounterDefinition GetRandomEncounter()
    {
        var gs = GameSession.I;
        if (gs == null) return null;
        return gs.PickRandomEncounter();
    }

    void CheckIfRunIsCompletePanel()
    {
        var gs = GameSession.I;
        if (gs != null && gs.bossDefeated)
        {
            if (runCompletePanel != null)
                runCompletePanel.SetActive(true);

            // Run is over - drop the persistent run UI (DeckView/Currency/Relics/Settings).
            SceneController.instance?.UnloadPersistentUi();
            return;
        }

        // Any other map visit this run (first entry after Start Run, or returning from a normal
        // encounter/shop): make sure the persistent run UI is up. No-op if it's already loaded.
        SceneController.instance?.LoadPersistentUi();
    }

    void OnRestartRunClicked()
    {
        MapState.ClearState();

        var gs = GameSession.I;
        if (gs != null)
        {
            gs.bossDefeated = false;
            gs.isBossBattle = false;
            gs.hasGrantedStartingTroop = false;
            gs.army.Clear();
        }

        SceneController.instance.GoTo(clanSelectSceneName);
    }
    
    public void DevJumpToBossBattleNow()
    {
        if (GS == null)
        {
            Debug.LogWarning("[MapGenerator] DevJumpToBossBattleNow failed: GameSession missing.");
            return;
        }

        MapNode bossNode = GetNodeAt(BossRowIndex, CenterColumn);
        if (bossNode == null)
        {
            Debug.LogWarning("[MapGenerator] DevJumpToBossBattleNow failed: boss node not found.");
            return;
        }

        // Move player directly to boss node.
        GS.mapCurrentRow = bossNode.row;
        GS.mapCurrentColumn = bossNode.column;

        // Make the boss node count as visited/selected just like normal map flow.
        bossNode.Visit();

        // Since we're going straight to boss, clear normal boss completion flags first.
        GS.isBossBattle = true;
        GS.bossDefeated = false;

        RefreshAvailableNodes();
        UpdateAllVisuals();
        SaveMapState();
        SnapTokenAndCameraToCurrent();

        StartCoroutine(NavigateToNodeScene(bossNode));
    }

    public void DevJumpToShopNow()
    {
        if (GS == null)
        {
            Debug.LogWarning("[MapGenerator] DevJumpToShopNow failed: GameSession missing.");
            return;
        }

        MapNode shopNode = GetFirstNodeOfType(MapNodeType.Shop);
        if (shopNode == null)
        {
            Debug.LogWarning("[MapGenerator] DevJumpToShopNow failed: no shop node found on this map.");
            return;
        }

        GS.mapCurrentRow = shopNode.row;
        GS.mapCurrentColumn = shopNode.column;

        shopNode.Visit();

        RefreshAvailableNodes();
        UpdateAllVisuals();
        SaveMapState();
        SnapTokenAndCameraToCurrent();

        StartCoroutine(NavigateToNodeScene(shopNode));
    }

    MapNode GetFirstNodeOfType(MapNodeType type)
    {
        for (int row = 0; row < mapRows.Count; row++)
        {
            for (int col = 0; col < mapRows[row].Count; col++)
            {
                MapNode node = mapRows[row][col];
                if (node != null && node.nodeType == type)
                    return node;
            }
        }

        return null;
    }

    public void DevJumpToBossNodeOnly()
    {
        if (GS == null)
        {
            Debug.LogWarning("[MapGenerator] DevJumpToBossNodeOnly failed: GameSession missing.");
            return;
        }

        MapNode bossNode = GetNodeAt(BossRowIndex, CenterColumn);
        if (bossNode == null)
        {
            Debug.LogWarning("[MapGenerator] DevJumpToBossNodeOnly failed: boss node not found.");
            return;
        }

        GS.mapCurrentRow = bossNode.row;
        GS.mapCurrentColumn = bossNode.column;

        bossNode.Visit();

        RefreshAvailableNodes();
        UpdateAllVisuals();
        SaveMapState();
        SnapTokenAndCameraToCurrent();
    }

    void OnQuitGameClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}