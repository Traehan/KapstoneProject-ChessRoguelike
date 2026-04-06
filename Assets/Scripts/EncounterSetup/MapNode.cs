using UnityEngine;
using Chess;

[System.Serializable]
public class MapNode
{
    public int row;
    public int column;
    public MapNodeType nodeType;

    public bool isVisited;
    public bool isCurrentlyAvailable;

    public bool isStartTile;
    public bool isBossTile;

    public EncounterDefinition encounter;

    public MapNode(int row, int column, MapNodeType nodeType)
    {
        this.row = row;
        this.column = column;
        this.nodeType = nodeType;

        isVisited = false;
        isCurrentlyAvailable = false;

        isStartTile = false;
        isBossTile = (nodeType == MapNodeType.Boss);
    }

    public void Visit()
    {
        isVisited = true;
        isCurrentlyAvailable = false;
    }

    public void SetAvailable(bool value)
    {
        if (isVisited && value)
        {
            isCurrentlyAvailable = false;
            return;
        }

        isCurrentlyAvailable = value;
    }

    public void SetAsStartTile()
    {
        isStartTile = true;
        isCurrentlyAvailable = false;
    }

    public void SetAsBossTile()
    {
        isBossTile = true;
        nodeType = MapNodeType.Boss;
    }

    public bool MatchesCoord(int targetRow, int targetColumn)
    {
        return row == targetRow && column == targetColumn;
    }
}