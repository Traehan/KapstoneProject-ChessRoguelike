// using System.Collections.Generic;
// using System.Linq;
// using UnityEngine;
//
// namespace Chess
// {
//     public partial class TurnManager
//     {
//         // =========================
//         //  RESTART ROUND (Start-of-turn rewind)
//         // =========================
//
//         class TurnStartSnapshot
//         {
//             public int ap;
//             public bool queenMovedThisTurn;
//             public HashSet<Piece> movedThisTurn = new HashSet<Piece>();
//             public List<PieceState> pieces = new List<PieceState>();
//         }
//
//         struct PieceState
//         {
//             public Piece piece;
//             public Vector2Int coord;
//             public int hp;
//             public int fortify;
//             public bool pawnHasMoved;
//         }
//
//         TurnStartSnapshot _turnStartSnapshot;
//
//         /// <summary>
//         /// Call this once at the START of the player turn (end of BeginPlayerTurn()).
//         /// </summary>
//         void CaptureTurnStartSnapshot()
//         {
//             if (board == null) return;
//
//             _turnStartSnapshot = new TurnStartSnapshot();
//             _turnStartSnapshot.ap = CurrentAP;
//             _turnStartSnapshot.queenMovedThisTurn = _queenMovedThisTurn;
//             _turnStartSnapshot.movedThisTurn = new HashSet<Piece>(_movedThisPlayerTurn);
//
//             _turnStartSnapshot.pieces.Clear();
//
//             // Snapshot every current piece on the board
//             foreach (var p in board.GetAllPieces())
//             {
//                 if (p == null) continue;
//
//                 var s = new PieceState
//                 {
//                     piece = p,
//                     coord = p.Coord,
//                     hp = p.currentHP,
//                     fortify = p.fortifyStacks,
//                     pawnHasMoved = (p is Pawn pw) && pw.hasMoved
//                 };
//
//                 _turnStartSnapshot.pieces.Add(s);
//             }
//         }
//
//         /// <summary>
//         /// Hook this to your UI button. Rewinds to the START of the current player turn.
//         /// </summary>
//         public void RestartRoundButton()
//         {
//             if (Phase != TurnPhase.PlayerTurn) return;
//             if (_turnStartSnapshot == null) return;
//             if (board == null) return;
//
//             RestoreTurnStartSnapshot();
//         }
//
//         void RestoreTurnStartSnapshot()
//         {
//             // 1) Clear highlights
//             board.ClearHighlights();
//
//             // 2) Remove everything currently on the board
//             var livePieces = board.GetAllPieces().ToList();
//             foreach (var p in livePieces)
//             {
//                 if (p == null) continue;
//                 board.RemovePiece(p);
//             }
//
//             // 3) Re-place all snapshot pieces
//             foreach (var s in _turnStartSnapshot.pieces)
//             {
//                 if (s.piece == null) continue;
//
//                 // If something actually got destroyed, skip it (can't restore a destroyed instance)
//                 // Unity destroyed objects compare equal to null.
//                 if (s.piece == null) continue;
//
//                 board.PlaceWithoutCapture(s.piece, s.coord);
//
//                 s.piece.currentHP = s.hp;
//                 s.piece.fortifyStacks = s.fortify;
//
//                 if (s.piece is Pawn pw)
//                     pw.hasMoved = s.pawnHasMoved;
//
//                 // Optional: if you want piece runtime to reset visuals/state, you can add a method later and call it here.
//                 // s.piece.GetComponent<PieceRuntime>()?.Notify_Undo();
//             }
//
//             // 4) Restore TurnManager state
//             CurrentAP = _turnStartSnapshot.ap;
//             OnAPChanged?.Invoke(CurrentAP, apPerTurn);
//
//             _queenMovedThisTurn = _turnStartSnapshot.queenMovedThisTurn;
//
//             _movedThisPlayerTurn.Clear();
//             foreach (var p in _turnStartSnapshot.movedThisTurn)
//             {
//                 if (p != null) _movedThisPlayerTurn.Add(p);
//             }
//
//             // // 5) Reset per-move undo stack (we rewound the whole turn)
//             // _moveStack.Clear();
//
//             // 6) Rebind queen + repaint overlays
//             EnsureQueenLeaderBound();
//             RecomputeEnemyIntentsAndPaint();
//             PaintAbilityHints();
//         }
//     }
// }
