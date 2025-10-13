using UnityEngine;
using System.Collections.Generic;
using Chess;

public class PlacementManager : MonoBehaviour
{
    public ChessBoard board;
    public Team playerTeam = Team.White;
    [Tooltip("Allowed rows (y indices) where player may place.")]
    public int[] allowedRows = new int[] { 0, 1 };

    HashSet<Vector2Int> _occupied = new();

    public bool CanPlace(PieceDefinition def, Vector2Int c)
    {
        if (board == null) return false;
        if (!board.InBounds(c)) return false;
        if (def == null || def.piecePrefab == null) return false;

        if (board.IsOccupied(c) || _occupied.Contains(c)) return false;

        bool rowOK = false;
        foreach (var y in allowedRows) if (c.y == y) { rowOK = true; break; }
        if (!rowOK) return false;

        return true;
    }

    public bool TryPlace(PieceDefinition def, Vector2Int c)
    {
        if (def == null) { Debug.LogError("PlacementManager.TryPlace: def is null"); return false; }
        if (def.piecePrefab == null) { Debug.LogError($"PieceDefinition '{def.name}' has no piecePrefab assigned."); return false; }
        if (!CanPlace(def, c)) { Debug.Log($"Cannot place {def.displayName} at {c}"); return false; }

        var placed = board.PlacePiece(def.piecePrefab, c, playerTeam);
        if (placed == null)
        {
            Debug.LogWarning($"Board refused placement at {c} (out of bounds or occupied).");
            return false;
        }

        _occupied.Add(c);
        return true;
    }
}