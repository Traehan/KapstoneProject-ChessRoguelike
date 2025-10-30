// Assets/Scripts/Chess/BoardInput.cs

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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
            if (!(Mouse.current?.leftButton.wasPressedThisFrame ?? false)) return;

            var mousePos = Mouse.current.position.ReadValue();
            var ray = Camera.main.ScreenPointToRay(mousePos);
            if (!Physics.Raycast(ray, out var hit, 100f)) return;

            var tm    = TurnManager.Instance;
            var piece = hit.collider.GetComponentInParent<Piece>();
            var tile  = hit.collider.GetComponentInParent<Tile>();

            // Helper to run after a successful player action
            void AfterSuccessfulAction()
            {
                board.ClearHighlights();
                // Keep your existing intent + Iron March layering:
                tm.RecomputeEnemyIntentsAndPaint(); // includes .RepaintEnemyIntentsOverlay()
                tm.RepaintIronMarchHints();
                _selected = null;
                _moves.Clear();
            }

            // === Clicked a piece ===
            if (piece != null)
            {
                // If we have a selection and clicked an enemy occupying a legal dest -> attack via move API
                if (_selected != null && tm != null && tm.IsPlayerTurn && piece.Team != tm.PlayerTeam)
                {
                    if (_moves.Contains(piece.Coord))
                    {
                        if (tm.TryPlayerAct_Move(_selected, piece.Coord))
                            AfterSuccessfulAction();
                    }
                    return;
                }

                // Selecting one of our pieces
                if (tm == null || !tm.IsPlayerTurn || piece.Team != tm.PlayerTeam) return;

                _selected = piece;
                board.ClearHighlights();

                // Draw enemy intents *first*, then our move options can visually override overlaps
                tm.RecomputeEnemyIntentsAndPaint();
                tm.RepaintIronMarchHints();

                _moves.Clear();
                _selected.GetLegalMoves(_moves);
                board.Highlight(_moves, highlightColor);
                return;
            }

            // === Clicked a tile ===
            if (tile != null && _selected != null)
            {
                if (_moves.Contains(tile.Coord))
                {
                    if (tm != null && tm.TryPlayerAct_Move(_selected, tile.Coord))
                        AfterSuccessfulAction();
                }
            }
        }
    }
}
