// PlacementManager.cs
using UnityEngine;
using System.Collections.Generic;
using Chess;

public class PlacementManager : MonoBehaviour
{
    public ChessBoard board;
    public Team playerTeam = Team.White;
    public int[] allowedRows = new int[] { 0, 1 };

    // NEW: track what the player placed during Preparation
    class PlacedRecord
    {
        public PieceDefinition def;
        public Vector2Int coord;
        public Piece instance;
    }
    readonly List<PlacedRecord> _placed = new();
    readonly HashSet<Vector2Int> _occupied = new();

    public bool CanPlace(PieceDefinition def, Vector2Int c)
    {
        if (board == null || def == null || def.piecePrefab == null) return false;
        if (!board.InBounds(c)) return false;
        if (board.IsOccupied(c) || _occupied.Contains(c)) return false;

        bool rowOK = false;
        foreach (var y in allowedRows) if (c.y == y) { rowOK = true; break; }
        return rowOK;
    }

    public bool TryPlace(PieceDefinition def, Vector2Int c)
    {
        if (!CanPlace(def, c)) { Debug.Log($"Cannot place {def?.displayName} at {c}"); return false; }

        var placed = board.PlacePiece(def.piecePrefab, c, playerTeam);
        if (placed == null) { Debug.LogWarning($"Board refused placement at {c}"); return false; }

        _occupied.Add(c);
        _placed.Add(new PlacedRecord { def = def, coord = c, instance = placed });
        return true;
    }

    // === NEW: Undo last placement (returns the definition so panel can restore one icon)
    public bool UndoLast(out PieceDefinition def)
    {
        def = null;
        if (_placed.Count == 0) return false;

        var rec = _placed[_placed.Count - 1];
        _placed.RemoveAt(_placed.Count - 1);
        def = rec.def;
        _occupied.Remove(rec.coord);

        // Remove from board
        if (!board.TryRemovePieceAt(rec.coord))
        {
            // Fallback: destroy instance if board doesn't remove it
            if (rec.instance != null) Destroy(rec.instance.gameObject);
        }
        return true;
    }

    // === NEW: Reset all placements (used by Reset button)
    public void ResetAll()
    {
        // remove from board first
        for (int i = _placed.Count - 1; i >= 0; i--)
        {
            var rec = _placed[i];
            if (!board.TryRemovePieceAt(rec.coord))
            {
                if (rec.instance != null) Destroy(rec.instance.gameObject);
            }
        }
        _placed.Clear();
        _occupied.Clear();
    }
}
