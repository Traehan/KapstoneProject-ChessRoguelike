using UnityEngine;
using System.Collections.Generic;
using Chess;

[System.Serializable]
public class MapNode
{
    public int row;
    public int column;
    public MapNodeType nodeType;
    public bool isVisited;
    public bool isLocked;
    public bool isCurrentlyAvailable;
    
    public List<MapNode> connectedNodes = new List<MapNode>();
    
    public EncounterDefinition encounter;
    
    public MapNode(int row, int column, MapNodeType nodeType)
    {
        this.row = row;
        this.column = column;
        this.nodeType = nodeType;
        this.isVisited = false;
        this.isLocked = false;
        this.isCurrentlyAvailable = row == 0;
    }

    public void Visit()
    {
        isVisited = true;
        isCurrentlyAvailable = false;
    }

    public void Lock()
    {
        isLocked = true;
        isCurrentlyAvailable = false;
    }

    public void Unlock()
    {
        isLocked = false;
    }

    public void MakeAvailable()
    {
        if (!isVisited && !isLocked)
        {
            isCurrentlyAvailable = true;
        }
    }
}
