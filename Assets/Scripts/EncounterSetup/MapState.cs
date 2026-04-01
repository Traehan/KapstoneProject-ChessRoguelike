using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class MapNodeData
{
    public int row;
    public int column;
    public MapNodeType nodeType;

    public bool isVisited;
    public bool isCurrentlyAvailable;

    public bool isStartTile;
    public bool isBossTile;
}

[System.Serializable]
public class MapRowData
{
    public List<MapNodeData> nodes = new List<MapNodeData>();
}

[System.Serializable]
public class MapMovementInventoryData
{
    public int rookCount = 0;
    public int bishopCount = 0;
    public int knightCount = 0;
    public int queenCount = 0;
}

[System.Serializable]
public class MapState
{
    [Header("Validation")]
    public bool isValid = true;

    [Header("Board Shape")]
    public int boardWidth = 5;
    public int playableRows = 8;
    public int totalRows = 9;

    [Header("Player Position")]
    public int currentPlayerRow = 0;
    public int currentPlayerColumn = 2;

    [Header("Movement")]
    public MapMovementType selectedMovementType = MapMovementType.Rook;
    public MapMovementInventoryData movementInventory = new MapMovementInventoryData();

    [Header("Board Data")]
    public List<MapRowData> rows = new List<MapRowData>();

    private const string SAVE_KEY = "MapState";

    public static void SaveState(MapState state)
    {
        if (state == null)
        {
            Debug.LogWarning("[MapState] SaveState called with null state.");
            return;
        }

        string json = JsonUtility.ToJson(state);
        Debug.Log($"[MapState] Saving state. json length = {json.Length}");

        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    public static MapState LoadState()
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY))
            return null;

        string json = PlayerPrefs.GetString(SAVE_KEY);
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            MapState state = JsonUtility.FromJson<MapState>(json);

            if (state == null)
            {
                Debug.LogWarning("[MapState] Loaded null state from json.");
                return null;
            }

            Debug.Log(
                $"[MapState] Loaded. " +
                $"valid={state.isValid}, " +
                $"boardWidth={state.boardWidth}, " +
                $"playableRows={state.playableRows}, " +
                $"totalRows={state.totalRows}, " +
                $"player=({state.currentPlayerRow},{state.currentPlayerColumn}), " +
                $"rows={state.rows?.Count ?? 0}, " +
                $"counts R={state.movementInventory?.rookCount ?? 0}, " +
                $"B={state.movementInventory?.bishopCount ?? 0}, " +
                $"K={state.movementInventory?.knightCount ?? 0}, " +
                $"Q={state.movementInventory?.queenCount ?? 0}"
            );

            return state;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[MapState] Failed to load MapState from PlayerPrefs. {ex.Message}");
            return null;
        }
    }

    public static void ClearState()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.Save();
        Debug.Log("[MapState] Cleared saved map state.");
    }
}