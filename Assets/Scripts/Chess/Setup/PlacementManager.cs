// PlacementManager.cs
using UnityEngine;
using System.Collections.Generic;
using Chess;
using Card;

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
        public Card.Card card;
        public Vector2Int coord;
        public Piece instance;
    }

    readonly List<PlacedRecord> _placed = new();
    readonly HashSet<Vector2Int> _occupied = new();

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

        foreach (var y in allowedRows)
            if (c.y == y)
                return true;

        return false;
    }

    public bool TryPlace(Card.Card card, Vector2Int c)
    {
        if (card == null) return false;

        var def = card.Definition;

        if (!CanPlace(def, c))
        {
            Debug.Log($"Cannot place {def?.displayName} at {c}");
            return false;
        }

        var placed = board.PlacePiece(def.piecePrefab, c, playerTeam);
        if (placed == null)
        {
            Debug.LogWarning($"Board refused placement at {c}");
            return false;
        }

        placed.EnsureDefinition(def);

        var runtime = placed.GetComponent<PieceRuntime>();
        if (runtime == null)
            runtime = placed.gameObject.AddComponent<PieceRuntime>();

        runtime.Init(placed, board, turnManager);

        _occupied.Add(c);
        _placed.Add(new PlacedRecord
        {
            card = card,
            coord = c,
            instance = placed
        });

        return true;
    }

    // Undo last placement (returns the Card)
    public bool UndoLast(out Card.Card card)
    {
        card = null;

        if (_placed.Count == 0)
            return false;

        var rec = _placed[_placed.Count - 1];
        _placed.RemoveAt(_placed.Count - 1);

        card = rec.card;
        _occupied.Remove(rec.coord);

        if (!board.TryRemovePieceAt(rec.coord))
        {
            if (rec.instance != null)
                Destroy(rec.instance.gameObject);
        }

        return true;
    }

    public void ResetAll()
    {
        for (int i = _placed.Count - 1; i >= 0; i--)
        {
            var rec = _placed[i];

            if (!board.TryRemovePieceAt(rec.coord))
            {
                if (rec.instance != null)
                    Destroy(rec.instance.gameObject);
            }
        }

        _placed.Clear();
        _occupied.Clear();
    }
}
