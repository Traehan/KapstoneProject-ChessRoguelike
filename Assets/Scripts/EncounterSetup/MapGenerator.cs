using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using Chess;
using GameManager;
using Random = UnityEngine.Random;
using UnityEngine.SceneManagement;

public class MapGenerator : MonoBehaviour
{
    [Header("Map Settings")]
    [Tooltip("Number of NORMAL rows in the map (boss row is added on top automatically).")]
    public int numberOfRows = 10;
    
    [Tooltip("Minimum nodes per row")]
    public int minNodesPerRow = 2;
    
    [Tooltip("Maximum nodes per row")]
    public int maxNodesPerRow = 4;
    
    [Header("Node Type Weights")]
    public MapNodeTypeWeights nodeTypeWeights = new MapNodeTypeWeights();
    
    [Header("Visual Settings")]
    public GameObject nodeVisualPrefab;
    public RectTransform contentParent;
    public float horizontalSpacing = 200f;
    public float verticalSpacing = 150f;
    
    [Header("Path Lines")]
    public GameObject pathLinePrefab;
    public Color activePathColor = Color.white;
    public Color inactivePathColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
    
    [Header("Scene References")]
    public string battleSceneName = "SampleScene";
    public string shopSceneName = "ShopScene";
    public string eventSceneName = "EventScene";

    [Header("Boss Settings")]
    [Tooltip("The encounter that will be used by the final Boss node at the top of the map.")]
    public EncounterDefinition bossEncounter;
    
    [Header("Run Complete UI")]
    [SerializeField] GameObject runCompletePanel;
    [SerializeField] Button restartRunButton;
    [SerializeField] Button quitGameButton;
    [SerializeField] string clanSelectSceneName = "ClanSelectScene";

    private List<List<MapNode>> mapRows = new List<List<MapNode>>();
    private Dictionary<MapNode, MapNodeVisual> nodeVisuals = new Dictionary<MapNode, MapNodeVisual>();
    private List<GameObject> pathLines = new List<GameObject>();
    private int currentRow = 0;
    
    private MapNode bossNode;

    void OnEnable()
    {
        Debug.Log($"[MapGenerator] OnEnable in scene {SceneManager.GetActiveScene().name}");
        LoadOrGenerateMap();
        
        if (runCompletePanel != null)
            runCompletePanel.SetActive(false);

        if (restartRunButton != null)
            restartRunButton.onClick.AddListener(OnRestartRunClicked);

        if (quitGameButton != null)
            quitGameButton.onClick.AddListener(OnQuitGameClicked);
        
        CheckIfRunIsCompletePanel();
    }
    
    void LoadOrGenerateMap()
    {
        MapState savedState = MapState.LoadState();

        if (savedState != null &&
            savedState.isValid &&
            savedState.rows != null &&
            savedState.rows.Count > 0)
        {
            Debug.Log("[MapGenerator] Restoring map from state");
            RestoreMapFromState(savedState);
        }
        else
        {
            Debug.Log("[MapGenerator] No valid saved map. Generating new one.");
            GenerateNewMap();
        }
    }

    void GenerateNewMap()
    {
        mapRows.Clear();
        ClearVisuals();

        // 1) Build NORMAL rows
        for (int row = 0; row < numberOfRows; row++)
        {
            int nodesInRow = Random.Range(minNodesPerRow, maxNodesPerRow + 1);
            List<MapNode> rowNodes = new List<MapNode>();

            for (int col = 0; col < nodesInRow; col++)
            {
                MapNodeType nodeType = nodeTypeWeights.GetRandomNodeType();
                MapNode node = new MapNode(row, col, nodeType);
                
                if (node.nodeType == MapNodeType.Encounter)
                {
                    node.encounter = GetRandomEncounter();
                }
                
                rowNodes.Add(node);
            }

            mapRows.Add(rowNodes);
        }

        // 2) Connect normal rows
        ConnectNodes();

        // 3) Add final Boss row and connect last normal row to it
        AddBossRowAndConnect();

        // 4) Build visuals + save
        CreateVisuals();
        SaveMapState();
    }

    void ConnectNodes()
    {
        for (int row = 0; row < mapRows.Count - 1; row++)
        {
            List<MapNode> currentRowNodes = mapRows[row];
            List<MapNode> nextRowNodes = mapRows[row + 1];

            foreach (MapNode currentNode in currentRowNodes)
            {
                int minConnections = 1;
                int maxConnections = Mathf.Min(2, nextRowNodes.Count);
                int connectionCount = Random.Range(minConnections, maxConnections + 1);

                List<MapNode> availableNextNodes = new List<MapNode>(nextRowNodes);
                
                for (int i = 0; i < connectionCount && availableNextNodes.Count > 0; i++)
                {
                    int randomIndex = Random.Range(0, availableNextNodes.Count);
                    MapNode targetNode = availableNextNodes[randomIndex];
                    
                    if (!currentNode.connectedNodes.Contains(targetNode))
                    {
                        currentNode.connectedNodes.Add(targetNode);
                    }
                    
                    availableNextNodes.RemoveAt(randomIndex);
                }
            }

            // ensure every node in the next row has at least one incoming
            foreach (MapNode nextNode in nextRowNodes)
            {
                bool hasIncomingConnection = false;
                foreach (MapNode currentNode in currentRowNodes)
                {
                    if (currentNode.connectedNodes.Contains(nextNode))
                    {
                        hasIncomingConnection = true;
                        break;
                    }
                }

                if (!hasIncomingConnection && currentRowNodes.Count > 0)
                {
                    MapNode randomCurrentNode = currentRowNodes[Random.Range(0, currentRowNodes.Count)];
                    randomCurrentNode.connectedNodes.Add(nextNode);
                }
            }
        }
    }

    /// <summary>
    /// Creates a single Boss row ABOVE the normal rows and connects
    /// every node in the last normal row to this Boss node.
    /// </summary>
    void AddBossRowAndConnect()
    {
        if (mapRows.Count == 0)
        {
            Debug.LogWarning("[MapGenerator] Cannot create boss row: no normal rows exist.");
            return;
        }

        // row index for boss is "numberOfRows" so it sits on top logically
        int bossRowIndex = numberOfRows;
        var bossRow = new List<MapNode>();

        MapNode boss = new MapNode(bossRowIndex, 0, MapNodeType.Boss);
        // Assign the boss encounter (or fallback to random so it never breaks)
        boss.encounter = bossEncounter != null ? bossEncounter : GetRandomEncounter();
        bossRow.Add(boss);
        bossNode = boss;

        // Connect ALL nodes from the last normal row to this Boss
        List<MapNode> lastNormalRow = mapRows[mapRows.Count - 1];
        foreach (MapNode node in lastNormalRow)
        {
            if (!node.connectedNodes.Contains(boss))
                node.connectedNodes.Add(boss);
        }

        mapRows.Add(bossRow);

        Debug.Log($"[MapGenerator] Boss row added at index {bossRowIndex}. Last normal row had {lastNormalRow.Count} nodes.");
    }

    void CreateVisuals()
    {
        if (nodeVisualPrefab == null || contentParent == null)
        {
            Debug.LogError("MapGenerator: Missing nodeVisualPrefab or contentParent!");
            return;
        }

        for (int row = 0; row < mapRows.Count; row++)
        {
            List<MapNode> rowNodes = mapRows[row];
            float totalWidth = (rowNodes.Count - 1) * horizontalSpacing;
            float startX = -totalWidth / 2f;

            for (int col = 0; col < rowNodes.Count; col++)
            {
                MapNode node = rowNodes[col];
                
                GameObject nodeObj = Instantiate(nodeVisualPrefab, contentParent);
                RectTransform rectTransform = nodeObj.GetComponent<RectTransform>();
                
                float xPos = startX + (col * horizontalSpacing);
                float yPos = -row * verticalSpacing;
                rectTransform.anchoredPosition = new Vector2(xPos, yPos);
                
                MapNodeVisual visual = nodeObj.GetComponent<MapNodeVisual>();
                if (visual != null)
                {
                    visual.Initialize(node, this);
                    nodeVisuals[node] = visual;
                }
                else
                {
                    Debug.LogError("MapGenerator: nodeVisualPrefab is missing MapNodeVisual component!");
                }
            }
        }

        CreatePathLines();
        AdjustContentSize();
    }

    void CreatePathLines()
    {
        if (pathLinePrefab == null) return;

        foreach (var pathLine in pathLines)
        {
            if (pathLine != null) Destroy(pathLine);
        }
        pathLines.Clear();

        for (int row = 0; row < mapRows.Count - 1; row++)
        {
            List<MapNode> currentRowNodes = mapRows[row];
            
            foreach (MapNode currentNode in currentRowNodes)
            {
                if (!nodeVisuals.ContainsKey(currentNode)) continue;
                
                RectTransform startRect = nodeVisuals[currentNode].GetComponent<RectTransform>();
                
                foreach (MapNode connectedNode in currentNode.connectedNodes)
                {
                    if (!nodeVisuals.ContainsKey(connectedNode)) continue;
                    
                    RectTransform endRect = nodeVisuals[connectedNode].GetComponent<RectTransform>();
                    
                    GameObject lineObj = Instantiate(pathLinePrefab, contentParent);
                    lineObj.transform.SetAsFirstSibling();
                    
                    RectTransform lineRect = lineObj.GetComponent<RectTransform>();
                    Image lineImage = lineObj.GetComponent<Image>();
                    
                    Vector2 startPos = startRect.anchoredPosition;
                    Vector2 endPos = endRect.anchoredPosition;
                    Vector2 direction = endPos - startPos;
                    float distance = direction.magnitude;
                    
                    lineRect.anchoredPosition = startPos + direction / 2f;
                    lineRect.sizeDelta = new Vector2(distance, 4f);
                    lineRect.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
                    
                    if (lineImage != null)
                    {
                        lineImage.color = inactivePathColor;
                    }
                    
                    pathLines.Add(lineObj);
                }
            }
        }
    }

    void AdjustContentSize()
    {
        if (contentParent == null) return;
        
        // Use actual mapRows.Count so the extra Boss row is always included
        float rowsCount = Mathf.Max(mapRows.Count, numberOfRows);
        float contentHeight = rowsCount * verticalSpacing + 200f;
        float contentWidth = (maxNodesPerRow) * horizontalSpacing + 200f;
        
        contentParent.sizeDelta = new Vector2(contentWidth, contentHeight);
    }

    void ClearVisuals()
    {
        foreach (var kvp in nodeVisuals)
        {
            if (kvp.Value != null && kvp.Value.gameObject != null)
            {
                Destroy(kvp.Value.gameObject);
            }
        }
        nodeVisuals.Clear();

        foreach (var pathLine in pathLines)
        {
            if (pathLine != null) Destroy(pathLine);
        }
        pathLines.Clear();
    }

    public void OnNodeSelected(MapNode selectedNode)
    {
        LockCurrentRow(selectedNode.row);
        selectedNode.Visit();
        UnlockConnectedNodes(selectedNode);
        currentRow = selectedNode.row + 1;
        
        UpdateAllVisuals();
        SaveMapState();
        
        StartCoroutine(NavigateToNodeScene(selectedNode));
    }

    void LockCurrentRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= mapRows.Count) return;
        
        foreach (MapNode node in mapRows[rowIndex])
        {
            node.Lock();
        }
    }

    void UnlockConnectedNodes(MapNode selectedNode)
    {
        foreach (MapNode connectedNode in selectedNode.connectedNodes)
        {
            connectedNode.Unlock();
            connectedNode.MakeAvailable();
        }
    }

    void UpdateAllVisuals()
    {
        foreach (var kvp in nodeVisuals)
        {
            if (kvp.Value != null)
            {
                kvp.Value.UpdateVisuals();
            }
        }
    }

    IEnumerator NavigateToNodeScene(MapNode node)
    {
        yield return new WaitForSeconds(0.3f);
        
        var gs = GameSession.I;
        if (gs == null)
        {
            Debug.LogError("GameSession missing in MapScene");
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
                    if (randomEncounter != null)
                    {
                        gs.selectedEncounter = randomEncounter;
                        sceneArgs = randomEncounter;
                    }
                    else
                    {
                        Debug.LogError("No encounter available!");
                        yield break;
                    }
                }
                break;
                
            case MapNodeType.Shop:
                targetScene = shopSceneName;
                break;
                
            case MapNodeType.RandomEvent:
                targetScene = eventSceneName;
                Debug.Log("Event scene not yet implemented - loading shop instead");
                targetScene = shopSceneName;
                break;

            case MapNodeType.Boss:
                targetScene = battleSceneName;
                // Boss should always use the bossEncounter if assigned
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
                    if (randomBossFallback != null)
                    {
                        gs.selectedEncounter = randomBossFallback;
                        sceneArgs = randomBossFallback;
                    }
                    else
                    {
                        Debug.LogError("No boss encounter available!");
                        yield break;
                    }
                }
                break;
        }

        yield return SceneController.instance.GoTo(targetScene, sceneArgs);
    }

    void SaveMapState()
    {
        Debug.Log("[MapGenerator] SaveMapState() called");

        MapState state = new MapState();
        state.currentRow = this.currentRow;
        state.numberOfRows = this.numberOfRows;   // still tracks NORMAL rows only
        state.rows = new List<MapRowData>();

        foreach (List<MapNode> row in mapRows)
        {
            MapRowData rowData = new MapRowData();

            foreach (MapNode node in row)
            {
                MapNodeData nodeData = new MapNodeData
                {
                    row = node.row,
                    column = node.column,
                    nodeType = node.nodeType,
                    isVisited = node.isVisited,
                    isLocked = node.isLocked,
                    isCurrentlyAvailable = node.isCurrentlyAvailable,
                    connectedNodeIndices = new List<int>()
                };

                foreach (MapNode connectedNode in node.connectedNodes)
                {
                    int index = mapRows[connectedNode.row].IndexOf(connectedNode);
                    nodeData.connectedNodeIndices.Add(connectedNode.row * 100 + index);
                }

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

        currentRow = state.currentRow;
        numberOfRows = state.numberOfRows;

        bossNode = null;

        foreach (MapRowData rowData in state.rows)
        {
            List<MapNode> row = new List<MapNode>();

            foreach (MapNodeData nodeData in rowData.nodes)
            {
                MapNode node = new MapNode(nodeData.row, nodeData.column, nodeData.nodeType)
                {
                    isVisited = nodeData.isVisited,
                    isLocked = nodeData.isLocked,
                    isCurrentlyAvailable = nodeData.isCurrentlyAvailable
                };

                if (node.nodeType == MapNodeType.Encounter)
                {
                    node.encounter = GetRandomEncounter();
                }
                else if (node.nodeType == MapNodeType.Boss)
                {
                    node.encounter = bossEncounter != null ? bossEncounter : GetRandomEncounter();
                    bossNode = node;
                }

                row.Add(node);
            }

            mapRows.Add(row);
        }

        // Rebuild connections using the indices
        foreach (MapRowData rowData in state.rows)
        {
            foreach (MapNodeData nodeData in rowData.nodes)
            {
                MapNode node = mapRows[nodeData.row][nodeData.column];
                node.connectedNodes.Clear();

                foreach (int encodedIndex in nodeData.connectedNodeIndices)
                {
                    int rowIndex = encodedIndex / 100;
                    int nodeIndex = encodedIndex % 100;
                    if (rowIndex >= 0 && rowIndex < mapRows.Count &&
                        nodeIndex >= 0 && nodeIndex < mapRows[rowIndex].Count)
                    {
                        node.connectedNodes.Add(mapRows[rowIndex][nodeIndex]);
                    }
                }
            }
        }

        CreateVisuals();
        UpdateAllVisuals();
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
            // Optional: lock map interaction here if needed
            if (runCompletePanel != null)
                runCompletePanel.SetActive(true);
        }
    }

    void OnRestartRunClicked()
    {
        // Clear map state from previous run
        MapState.ClearState();

        var gs = GameSession.I;
        if (gs != null)
        {
            gs.bossDefeated = false;
            gs.isBossBattle = false;
            gs.hasGrantedStartingTroop = false;
            gs.army.Clear();
            // If you have other per-run systems (currency, etc.) reset them here too
            // e.g. CurrencyManager.Instance?.ResetForNewRun();
        }

        // Go back to Clan Select scene
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
