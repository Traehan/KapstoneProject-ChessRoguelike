// using System.Collections.Generic;
// using System.Linq;
// using UnityEngine;
//
// namespace Chess
// {
//     public partial class TurnManager
//     {
//         bool TryMoveOrAttack(Piece mover, Vector2Int to)
//         {
//             if (!board.InBounds(to)) return false;
//
//             if (!board.TryGetPiece(to, out var target))
//             {
//                 if (!board.TryMovePiece(mover, to)) return false;
//                 HandlePostMoveBookkeeping(mover);
//                 HandleEnemyBackRankEscapeIfNeeded(mover);
//                 return true;
//             }
//
//             if (target.Team == mover.Team) return false;
//
//             bool enemyTurn = Phase == TurnPhase.EnemyTurn && mover.Team == enemyTeam;
//             ResolveCombat(mover, target, !enemyTurn, out bool aDied, out bool dDied);
//
//             if (dDied) board.RemovePiece(target);
//             if (aDied) board.RemovePiece(mover);
//             if (enemyTurn && !aDied && dDied)
//                 board.TryMovePiece(mover, to);
//
//             return true;
//         }
//
//         List<Piece> GetSortedEnemies()
//         {
//             return board.GetAllPieces()
//                 .Where(p => p.Team == enemyTeam)
//                 .OrderBy(p => p.Coord.y)
//                 .ThenBy(p => p.Coord.x)
//                 .ToList();
//         }
//
//         bool TryResolveEnemyTarget(Piece enemy, out Vector2Int target)
//         {
//             if (enemy is EnemyKnight ek && ek.HasLockedIntent)
//             {
//                 target = ek.LockedIntent;
//                 ek.ClearLockedIntent();
//                 return true;
//             }
//
//             target = default;
//             if (!enemy.TryGetComponent<IEnemyBehavior>(out var beh)) return false;
//             return beh.TryGetDesiredDestination(board, out target);
//         }
//
//         void HandlePostMoveBookkeeping(Piece mover)
//         {
//             if (Phase != TurnPhase.PlayerTurn) return;
//             _movedThisPlayerTurn.Add(mover);
//             mover.ClearFortify();
//             NotifyAbilitiesPieceMoved(mover);
//         }
//
//         void HandleEnemyBackRankEscapeIfNeeded(Piece mover)
//         {
//             if (Phase == TurnPhase.EnemyTurn && EnemyReachedPlayerSide(mover))
//                 LoseLifeAndDespawn(mover);
//         }
//     }
// }
