// Assets/Scripts/Chess/BoardInput.cs

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Chess
{
    public class BoardInput : MonoBehaviour
    {
        public ChessBoard board;
        public Color highlightColor = new Color(0.2f, 0.6f, 1f, .5f);

        Piece _selected;
        readonly List<Vector2Int> _moves = new();

        PieceRuntime _inspectedRuntime;

        void Update()
        {
            HandlePieceInspection();
            HandleLeftClickGameplay();
        }

        void HandlePieceInspection()
        {
            bool rightHeld = Mouse.current?.rightButton.isPressed ?? false;

            if (!rightHeld)
            {
                if (_inspectedRuntime != null)
                {
                    PieceInfoPanel.Instance?.Hide();
                    _inspectedRuntime = null;
                }
                return;
            }

            if (Camera.main == null)
                return;

            var mousePos = Mouse.current.position.ReadValue();
            var ray = Camera.main.ScreenPointToRay(mousePos);

            if (!Physics.Raycast(ray, out var hit, 100f))
            {
                if (_inspectedRuntime != null)
                {
                    PieceInfoPanel.Instance?.Hide();
                    _inspectedRuntime = null;
                }
                return;
            }

            var piece = hit.collider.GetComponentInParent<Piece>();
            if (piece == null)
            {
                if (_inspectedRuntime != null)
                {
                    PieceInfoPanel.Instance?.Hide();
                    _inspectedRuntime = null;
                }
                return;
            }

            var runtime = piece.GetComponent<PieceRuntime>();
            if (runtime == null)
            {
                if (_inspectedRuntime != null)
                {
                    PieceInfoPanel.Instance?.Hide();
                    _inspectedRuntime = null;
                }
                return;
            }

            if (_inspectedRuntime != runtime)
            {
                _inspectedRuntime = runtime;
                PieceInfoPanel.Instance?.Show(runtime);
            }
        }

        void HandleLeftClickGameplay()
        {
            if (!(Mouse.current?.leftButton.wasPressedThisFrame ?? false))
                return;

            if (Camera.main == null)
                return;

            var mousePos = Mouse.current.position.ReadValue();
            var ray = Camera.main.ScreenPointToRay(mousePos);

            if (!Physics.Raycast(ray, out var hit, 100f))
                return;

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
                            tm.RecomputeEnemyIntentsAndPaint();
                            tm.RepaintIronMarchHints();
                            _selected = null;
                            _moves.Clear();
                        }
                    }
                    return;
                }

                // Otherwise: selecting our own piece
                if (tm == null || !tm.IsPlayerTurn || piece.Team != tm.PlayerTeam)
                    return;

                _selected = piece;
                board.ClearHighlights();
                tm.RecomputeEnemyIntentsAndPaint();
                tm.RepaintIronMarchHints();

                _moves.Clear();
                _selected.GetLegalMoves(_moves);
                board.Highlight(_moves, highlightColor);
                return;
            }

            // Clicked a tile (no piece)
            if (tile != null && _selected != null)
            {
                if (_moves.Contains(tile.Coord))
                {
                    var tm = TurnManager.Instance;
                    if (tm != null && tm.TryPlayerAct_Move(_selected, tile.Coord))
                    {
                        board.ClearHighlights();
                        tm.RepaintEnemyIntentsOverlay();
                        tm.RepaintIronMarchHints();
                        _selected = null;
                        _moves.Clear();
                    }
                }
            }
        }
    }
}