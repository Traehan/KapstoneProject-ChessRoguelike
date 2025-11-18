using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class MapNodeData
{
    public int row;
    public int column;
    public MapNodeType nodeType;
    public bool isVisited;
    public bool isLocked;
    public bool isCurrentlyAvailable;
    public List<int> connectedNodeIndices = new List<int>();
}

[System.Serializable]
public class MapRowData
{
    public List<MapNodeData> nodes = new List<MapNodeData>();
}

[System.Serializable]
public class MapState
{
    public int currentRow;
    public int numberOfRows;
    public List<MapRowData> rows = new List<MapRowData>();
    public bool isValid = true;

    private const string SAVE_KEY = "MapState";

    public static void SaveState(MapState state)
    {
        string json = JsonUtility.ToJson(state);
        Debug.Log($"[MapState] Saving state. json length = {json.Length}");
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    public static MapState LoadState()
    {
        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            string json = PlayerPrefs.GetString(SAVE_KEY);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    MapState state = JsonUtility.FromJson<MapState>(json);
                    Debug.Log($"[MapState] Loaded: isValid={state.isValid}, rows={state.rows?.Count ?? 0}");
                    return state;
                }
                catch
                {
                    Debug.LogWarning("Failed to load MapState from PlayerPrefs");
                    return null;
                }
            }
        }
        return null;
    }

    public static void ClearState()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.Save();
    }
}