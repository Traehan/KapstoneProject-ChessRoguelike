using System.Collections;
using UnityEngine;

namespace Chess
{
    public partial class TurnManager
    {
        void SetPhase(TurnPhase next)
        {
            Phase = next;
            OnPhaseChanged?.Invoke(Phase);
            GameEvents.OnPhaseChanged?.Invoke(Phase);
        }


        void BeginPreparation()
        {
            deckManager.InitializeBattleDeck();
            deckManager.DrawPrepCards();
            
            SetPhase(TurnPhase.Preparation);
        }

        void BeginPlayerTurn()
        {
            SetPhase(TurnPhase.PlayerTurn);
            _history.Clear();

            CurrentAP = apPerTurn;
            deckManager.DrawUpTo(4);

            Debug.Log("=== COMBAT HAND ===");
            foreach (var card in deckManager.Hand)
            {
                Debug.Log("Card in hand: " + card.Definition.displayName);
            }
            GameEvents.OnAPChanged?.Invoke(CurrentAP, apPerTurn);

            _movedThisPlayerTurn.Clear();
            _queenMovedThisTurn = false;

            EnsureQueenLeaderBound();
            RecomputeEnemyIntentsAndPaint();

            NotifyAbilitiesBeginPlayerTurn();
            NotifyAllPlayerPieceRuntimes_BeginTurn();

            PaintAbilityHints();

            // IMPORTANT: this enables the restart-turn button to restore the turn start
            CaptureTurnStartSnapshot();
        }

        IEnumerator EnemyTurnRoutine()
        {
            SetPhase(TurnPhase.EnemyTurn);

            foreach (var enemy in GetSortedEnemies())
            {
                if (enemy == null || !board.ContainsPiece(enemy)) continue;

                if (enemy.TryGetComponent<BossEnemy>(out var boss))
                {
                    boss.ExecuteTurn(board);

                    if (enemyMoveDelay > 0f)
                        yield return new WaitForSeconds(enemyMoveDelay);

                    continue;
                }

                if (!TryResolveEnemyTarget(enemy, out var target)) continue;

                ExecuteEnemyActionAsCommand(enemy, target);

                if (enemyMoveDelay > 0f)
                    yield return new WaitForSeconds(enemyMoveDelay);
            }
            
            void ExecuteEnemyActionAsCommand(Piece enemy, Vector2Int target)
            {
                if (board == null || enemy == null) return;
                if (!board.ContainsPiece(enemy)) return;
                if (!board.InBounds(target)) return;

                var from = enemy.Coord;

                // Re-fetch what's currently on the target square NOW
                bool occupied = board.TryGetPiece(target, out var there) && there != null;

                // Empty -> move
                if (!occupied)
                {
                    var cmd = new EnemyMoveCommand(board, enemy, from, target);
                    if (cmd.Execute())
                        GameEvents.OnCommandExecuted?.Invoke(cmd);
                    return;
                }

                // Ally blocks
                if (there.Team == enemy.Team) return;

                // Attack
                var atk = new EnemyAttackCommand(this, board, enemy, there, from, target);
                if (atk.Execute())
                    GameEvents.OnCommandExecuted?.Invoke(atk);
            }



            SetPhase(TurnPhase.Cleanup);

            if (postEnemyTurnPause > 0f)
                yield return new WaitForSeconds(postEnemyTurnPause);

            EnsureEncounterRunnerBound();
            if (encounterRunner != null && encounterRunner.IsVictoryReady(board))
            {
                PlayerWon();
                yield break;
            }

            board.ClearHighlights();
            BeginPlayerTurn();
        }
    }
}
