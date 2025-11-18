// PlacementManager.cs
using UnityEngine;
using System.Collections.Generic;
using Chess;

public class PlacementManager : MonoBehaviour
{
    [Header("Board & Team")]
    public ChessBoard board;
    public Team playerTeam = Team.White;

    [Header("Allowed Rows (y) for Placement")]
    public int[] allowedRows = new int[] { 0, 1 };

    // Track what the player placed during Preparation
    class PlacedRecord
    {
        public PieceDefinition def;
        public Vector2Int coord;
        public Piece instance;
    }
    readonly List<PlacedRecord> _placed = new();
    readonly HashSet<Vector2Int> _occupied = new();

    // NEW: TurnManager so we can init PieceRuntime
    [SerializeField] TurnManager turnManager;

    void Awake()
    {
        if (turnManager == null) turnManager = FindObjectOfType<TurnManager>();
        if (board == null) board = FindObjectOfType<ChessBoard>();
    }

    public bool CanPlace(PieceDefinition def, Vector2Int c)
    {
        if (board == null || def == null || def.piecePrefab == null) return false;
        if (!board.InBounds(c)) return false;
        if (board.IsOccupied(c) || _occupied.Contains(c)) return false;

        bool rowOK = false;
        foreach (var y in allowedRows) { if (c.y == y) { rowOK = true; break; } }
        return rowOK;
    }

    public bool TryPlace(PieceDefinition def, Vector2Int c)
    {
        if (!CanPlace(def, c))
        {
            Debug.Log($"Cannot place {def?.displayName} at {c}");
            return false;
        }

        // Ask the board to place the prefab; expect a Piece back
        var placed = board.PlacePiece(def.piecePrefab, c, playerTeam);
        if (placed == null)
        {
            Debug.LogWarning($"Board refused placement at {c}");
            return false;
        }

        // 🔹 IMPORTANT: make sure this instance uses the *runtime* PieceDefinition
        // that the shop/army is working with (so ConsumeUpgradesFor finds the right key).
        placed.EnsureDefinition(def);

        // --- ensure each spawned piece has a PieceRuntime and initialize it ---
        var go = placed.gameObject;
        var runtime = go.GetComponent<PieceRuntime>();
        if (runtime == null) runtime = go.AddComponent<PieceRuntime>();
        runtime.Init(placed, board, turnManager);   // innate abilities + slots seeded here

        // Bookkeeping for local undo/reset
        _occupied.Add(c);
        _placed.Add(new PlacedRecord { def = def, coord = c, instance = placed });

        return true;
    }


    // Undo last placement (returns the definition so the Prep panel can restore an icon)
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
            // Fallback: destroy instance if the board didn't remove it
            if (rec.instance != null) Destroy(rec.instance.gameObject);
        }
        return true;
    }

    // Reset all placements (used by a Reset button)
    public void ResetAll()
    {
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
