// using System.Collections;
// using UnityEngine;
//
// namespace Chess
// {
//     public partial class TurnManager
//     {
//         void SetPhase(TurnPhase next)
//         {
//             Phase = next;
//             OnPhaseChanged?.Invoke(Phase);
//         }
//
//         void BeginPreparation()
//         {
//             SetPhase(TurnPhase.Preparation);
//         }
//
//         void BeginPlayerTurn()
//         {
//             _moveStack.Clear();
//             SetPhase(TurnPhase.PlayerTurn);
//
//             CurrentAP = apPerTurn;
//             OnAPChanged?.Invoke(CurrentAP, apPerTurn);
//
//             _movedThisPlayerTurn.Clear();
//             _queenMovedThisTurn = false;
//
//             EnsureQueenLeaderBound();
//             RecomputeEnemyIntentsAndPaint();
//             NotifyAbilitiesBeginPlayerTurn();
//             NotifyAllPlayerPieceRuntimes_BeginTurn();
//             PaintAbilityHints();
//             CaptureTurnStartSnapshot();
//         }
//
//         IEnumerator EnemyTurnRoutine()
//         {
//             _moveStack.Clear();
//             SetPhase(TurnPhase.EnemyTurn);
//
//             foreach (var enemy in GetSortedEnemies())
//             {
//                 if (enemy == null || !board.ContainsPiece(enemy)) continue;
//
//                 if (enemy.TryGetComponent<BossEnemy>(out var boss))
//                 {
//                     boss.ExecuteTurn(board);
//                     yield return new WaitForSeconds(enemyMoveDelay);
//                     continue;
//                 }
//
//                 if (!TryResolveEnemyTarget(enemy, out var target)) continue;
//                 TryMoveOrAttack(enemy, target);
//                 yield return new WaitForSeconds(enemyMoveDelay);
//             }
//
//             SetPhase(TurnPhase.Cleanup);
//             yield return new WaitForSeconds(postEnemyTurnPause);
//
//             EnsureEncounterRunnerBound();
//             if (encounterRunner != null && encounterRunner.IsVictoryReady(board))
//             {
//                 PlayerWon();
//                 yield break;
//             }
//
//             board.ClearHighlights();
//             BeginPlayerTurn();
//         }
//     }
// }