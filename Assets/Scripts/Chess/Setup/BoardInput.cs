// Assets/Scripts/Chess/BoardInput.cs

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
// <-- new system

namespace Chess
{
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

            // BoardInput.cs (inside Update, after raycast)
            var piece = hit.collider.GetComponentInParent<Piece>();
            var tile  = hit.collider.GetComponentInParent<Tile>();

            if (piece != null)
            {
                var tm = TurnManager.Instance;

                // If we already selected our piece, and we clicked an enemy that sits on a legal dest -> attack
                if (_selected != null && tm != null && tm.IsPlayerTurn && piece.Team != tm.PlayerTeam)
                {
                    if (_moves.Contains(piece.Coord))
                    {
                        if (tm.TryPlayerAct_Move(_selected, piece.Coord))
                        {
                            board.ClearHighlights();
                            tm.RecomputeEnemyIntentsAndPaint(); //highlights enemy intent
                            _selected = null;
                            _moves.Clear();
                        }
                    }
                    return; // handled
                }

                // Otherwise: selecting our own piece
                if (tm == null || !tm.IsPlayerTurn || piece.Team != tm.PlayerTeam) return;

                _selected = piece;
                board.ClearHighlights();
                // redraw enemy intents first (so your move options can visually override on overlap)
                TurnManager.Instance?.RepaintEnemyIntentsOverlay();
                _moves.Clear();
                _selected.GetLegalMoves(_moves);
                board.Highlight(_moves, highlightColor);
                return;
            }

// Clicked a tile (no piece), same as before
            if (tile != null && _selected != null)
            {
                if (_moves.Contains(tile.Coord))
                {
                    var tm = TurnManager.Instance;
                    if (tm != null && tm.TryPlayerAct_Move(_selected, tile.Coord))
                    {
                        board.ClearHighlights();                    // wipe old move highlights
                        tm.RecomputeEnemyIntentsAndPaint();         // refresh red intents for new board state
                        _selected = null;
                        _moves.Clear();
                    }
                }
            }

        }
    }
}