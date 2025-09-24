// Assets/Scripts/Chess/BoardInput.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // <-- new system
using ChessRL;

public class BoardInput : MonoBehaviour
{
    public ChessBoard board;
    public Color highlightColor = new Color(0.2f, 0.6f, 1f, 1f);

    Piece _selected;
    readonly List<Vector2Int> _moves = new();

    void Update()
    {
        // New Input System equivalents
        if (!(Mouse.current?.leftButton.wasPressedThisFrame ?? false)) return;

        var mousePos = Mouse.current.position.ReadValue();
        var ray = Camera.main.ScreenPointToRay(mousePos);

        if (!Physics.Raycast(ray, out var hit, 100f)) return;

        var piece = hit.collider.GetComponentInParent<Piece>();
        var tile  = hit.collider.GetComponentInParent<Tile>();

        if (piece != null)
        {
            _selected = piece;
            board.ClearHighlights();
            _moves.Clear();
            _selected.GetLegalMoves(_moves);
            board.Highlight(_moves, highlightColor);
            return;
        }

        if (tile != null && _selected != null)
        {
            if (_moves.Contains(tile.Coord)) board.MovePiece(_selected, tile.Coord);
            board.ClearHighlights();
            _selected = null;
            _moves.Clear();
        }
    }
}