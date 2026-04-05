using System.Collections;
using System.Collections.Generic;
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

    [Header("Visual Settings")]
    public GameObject nodeVisualPrefab;
    public RectTransform contentParent;
    public float horizontalSpacing = 200f;
    public float verticalSpacing = 150f;
    [SerializeField] ScrollRect mapScrollRect;

    [Header("Scene References")]
    public string battleSceneName = "SampleScene";
    public string shopSceneName = "ShopScene";
    public string eventSceneName = "EventScene";

    [Header("Boss Settings")]
    public EncounterDefinition bossEncounter;

    [Header("Run Complete UI")]
    [SerializeField] GameObject runCompletePanel;
    [SerializeField] Button restartRunButton;
    [SerializeField] Button quitGameButton;
    [SerializeField] string clanSelectSceneName = "ClanSelectScene";
    [Header("Node Panels")]
    [SerializeField] RecruitNodePanel recruitNodePanel;

    readonly List<List<MapNode>> mapRows = new();
    readonly Dictionary<MapNode, MapNodeVisual> nodeVisuals = new();

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

    void GenerateNewMap()
    {
        mapRows.Clear();
        ClearVisuals();

        if (GS != null)
        {
            GS.mapCurrentRow = 0;
            GS.mapCurrentColumn = CenterColumn;
            GS.selectedMapMovementType = MapMovementType.Rook;

            GS.rookMapMoveCount = 99;
            GS.bishopMapMoveCount = 99;
            GS.knightMapMoveCount = 99;
            GS.queenMapMoveCount = 0;
        }

        for (int row = 0; row < TotalRows; row++)
        {
            List<MapNode> rowNodes = new List<MapNode>();

            for (int col = 0; col < boardWidth; col++)
            {
                MapNode node = BuildNode(row, col);
                rowNodes.Add(node);
            }

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
    }

    IEnumerator CenterScrollOnCurrentNodeAfterLayout()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        yield return null;

        if (mapScrollRect == null)
            mapScrollRect = GetComponentInChildren<ScrollRect>();

        if (mapScrollRect == null || contentParent == null || mapScrollRect.viewport == null)
            yield break;

        MapNode currentNode = GetNodeAt(GetCurrentRow(), GetCurrentColumn());
        if (currentNode == null || !nodeVisuals.TryGetValue(currentNode, out var visual) || visual == null)
            yield break;

        RectTransform nodeRect = visual.GetComponent<RectTransform>();
        float contentHeight = contentParent.rect.height;
        float viewportHeight = mapScrollRect.viewport.rect.height;
        float maxScroll = Mathf.Max(0f, contentHeight - viewportHeight);

        if (maxScroll <= 0f)
        {
            mapScrollRect.verticalNormalizedPosition = 1f;
            yield break;
        }

        float nodeYFromTop = -nodeRect.anchoredPosition.y;
        float targetScrollY = nodeYFromTop - (viewportHeight * 0.5f);
        targetScrollY = Mathf.Clamp(targetScrollY, 0f, maxScroll);

        float normalized = 1f - (targetScrollY / maxScroll);
        mapScrollRect.verticalNormalizedPosition = normalized;
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

    void CreateVisuals()
    {
        if (nodeVisualPrefab == null || contentParent == null)
        {
            Debug.LogError("[MapGenerator] Missing nodeVisualPrefab or contentParent.");
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

                GameObject nodeObj = Instantiate(nodeVisualPrefab, contentParent);
                RectTransform rectTransform = nodeObj.GetComponent<RectTransform>();

                float centeredX = (col - (boardWidth - 1) * 0.5f) * horizontalSpacing;
                float visualRow = (TotalRows - 1) - row;
                float yPos = -visualRow * verticalSpacing;
                rectTransform.anchoredPosition = new Vector2(centeredX, yPos);

                MapNodeVisual visual = nodeObj.GetComponent<MapNodeVisual>();
                if (visual == null)
                {
                    Debug.LogError("[MapGenerator] nodeVisualPrefab is missing MapNodeVisual.");
                    continue;
                }

                visual.Initialize(node, this);
                nodeVisuals[node] = visual;
            }
        }

        AdjustContentSize();
        UpdateAllVisuals();
    }

    void AdjustContentSize()
    {
        if (contentParent == null) return;

        float contentHeight = TotalRows * verticalSpacing + 450f;
        float contentWidth = boardWidth * horizontalSpacing + 250f;
        contentParent.sizeDelta = new Vector2(contentWidth, contentHeight);
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

        GS.mapCurrentRow = selectedNode.row;
        GS.mapCurrentColumn = selectedNode.column;

        selectedNode.Visit();

        RefreshAvailableNodes();
        UpdateAllVisuals();
        SaveMapState();
        // StartCoroutine(CenterScrollOnCurrentNodeAfterLayout());

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

        yield return new WaitForSeconds(0.2f);

        var gs = GameSession.I;
        if (gs == null)
        {
            Debug.LogError("[MapGenerator] GameSession missing in MapScene.");
            yield break;
        }

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
                if (DeckViewController.Instance == null)
                {
                    Debug.LogWarning("[MapGenerator] No DeckViewController found for RemoveTwoCards event.");
                    yield break;
                }

                DeckViewController.Instance.OpenRemoveTwoMode();
                yield break;

            case MapNodeType.DuplicateCard:
                if (DeckViewController.Instance == null)
                {
                    Debug.LogWarning("[MapGenerator] No DeckViewController found for DuplicateCard event.");
                    yield break;
                }

                DeckViewController.Instance.OpenDuplicateOneMode();
                yield break;
        }

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
                    isBossTile = node.isBossTile
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
            GS.selectedMapMovementType = state.selectedMovementType;

            var inv = state.movementInventory ?? new MapMovementInventoryData();
            GS.rookMapMoveCount = inv.rookCount;
            GS.bishopMapMoveCount = inv.bishopCount;
            GS.knightMapMoveCount = inv.knightCount;
            GS.queenMapMoveCount = inv.queenCount;
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
                    isBossTile = savedNode.isBossTile
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
        StartCoroutine(CenterScrollOnCurrentNodeAfterLayout());
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
        }
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

    void OnQuitGameClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}