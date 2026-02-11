// using UnityEngine;
//
// namespace Chess
// {
//     public partial class TurnManager
//     {
//         bool ValidatePlayerMove(Piece piece, Vector2Int dest)
//         {
//             return Phase == TurnPhase.PlayerTurn &&
//                    piece != null &&
//                    board != null &&
//                    CurrentAP > 0 &&
//                    board.InBounds(dest);
//         }
//
//         bool HandlePlayerMoveToEmpty(Piece piece, Vector2Int from, Vector2Int dest,
//             int hpBefore, bool pawnMoved)
//         {
//             int fort = piece.fortifyStacks;
//             bool wasMarked = _movedThisPlayerTurn.Contains(piece);
//             bool queenMoved = _queenMovedThisTurn;
//
//             if (!board.TryMovePiece(piece, dest)) return false;
//
//             _movedThisPlayerTurn.Add(piece);
//             piece.ClearFortify();
//             if (piece == queenLeader) _queenMovedThisTurn = true;
//
//             piece.GetComponent<PieceRuntime>()?.Notify_PieceMoved(from, dest);
//             NotifyAbilitiesPieceMoved(piece);
//             PaintAbilityHints();
//
//             SpendAP(1);
//
//             _moveStack.Add(new LastPlayerMove
//             {
//                 mover = piece,
//                 from = from,
//                 to = dest,
//                 apSpent = 1,
//                 moverHP_Before = hpBefore,
//                 moverPawn_HadMovedBefore = pawnMoved,
//                 moverActuallyMoved = true,
//                 moverFortify_Before = fort,
//                 moverWasMarkedMoved_Before = wasMarked,
//                 queenMovedThisTurn_Before = queenMoved
//             });
//
//             RecomputeEnemyIntentsAndPaint();
//             return true;
//         }
//
//         bool HandlePlayerAttack(Piece attacker, Piece defender, Vector2Int from, Vector2Int dest,
//             int hpBefore, bool pawnMoved)
//         {
//             int defHP = defender.currentHP;
//
//             ResolveCombat(attacker, defender, true, out bool aDied, out bool dDied);
//
//             if (aDied)
//             {
//                 board.RemovePiece(attacker);
//                 SpendAP(1);
//                 _moveStack.Clear();
//                 return true;
//             }
//
//             if (dDied) board.CapturePiece(defender);
//
//             SpendAP(1);
//
//             _moveStack.Add(new LastPlayerMove
//             {
//                 mover = attacker,
//                 from = from,
//                 to = from,
//                 apSpent = 1,
//                 moverHP_Before = hpBefore,
//                 moverPawn_HadMovedBefore = pawnMoved,
//                 moverActuallyMoved = false,
//                 defender = defender,
//                 defenderAt = dest,
//                 defenderHP_Before = defHP,
//                 defenderDied = dDied
//             });
//
//             RecomputeEnemyIntentsAndPaint();
//             return true;
//         }
//
//         void SpendAP(int amt)
//         {
//             CurrentAP = Mathf.Max(0, CurrentAP - amt);
//             OnAPChanged?.Invoke(CurrentAP, apPerTurn);
//         }
//     }
// }
