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
            SetPhase(TurnPhase.Preparation);
            if (GameSession.I != null && deckManager != null)
                deckManager.InitializeBattleFromCardDefinitions(GameSession.I.CurrentRunDeck);
        }
        
        public void BeginEncounterFromPreparation()
        {
            if (Phase != TurnPhase.Preparation) return;

            // if (GameSession.I != null && deckManager != null)
            //     deckManager.InitializeBattleFromCardDefinitions(GameSession.I.CurrentRunDeck);
            
            EnsureQueenLeaderBound();
            BeginSpellPhase(); 
        }
        
        void BeginSpellPhase()
        {
            EnsureQueenLeaderBound();
            SetPhase(TurnPhase.SpellPhase);

            if (handPanel != null)
                handPanel.gameObject.SetActive(true);

            RefillManaForSpellPhase();

            deckManager?.DrawUpTo(4);

            int queuedDraws = ConsumeQueuedNextSpellPhaseDraws();
            if (queuedDraws > 0 && deckManager != null)
                deckManager.Draw(queuedDraws);
        }

        public void EndSpellPhaseButton()
        {
            if (Phase != TurnPhase.SpellPhase) return;

            deckManager?.DiscardEndOfTurn();

            if (handPanel != null)
                handPanel.PlayBulkDiscardSequenceAndHide();

            BeginPlayerTurn();
        }

        void BeginPlayerTurn()
        {
            SetPhase(TurnPhase.PlayerTurn);
            _history.Clear();

            CurrentAPMax = apPerTurn + Mathf.Max(0, _pendingNextBattlePhaseAPBonus);
            CurrentAP = CurrentAPMax;
            _pendingNextBattlePhaseAPBonus = 0;

            OnAPChanged?.Invoke(CurrentAP, CurrentAPMax);
            GameEvents.OnAPChanged?.Invoke(CurrentAP, CurrentAPMax);

            _movedThisPlayerTurn.Clear();
            _queenMovedThisTurn = false;
            
            RecomputeEnemyIntentsAndPaint();

            NotifyAbilitiesBeginPlayerTurn();
            NotifyAllPlayerPieceRuntimes_BeginTurn();

            // PaintAbilityHints();

            CaptureTurnStartSnapshot();
        }

        IEnumerator EnemyTurnRoutine()
        {
            SetPhase(TurnPhase.EnemyTurn);
            NotifyAllPieceRuntimes_BeginEnemyTurn();

            foreach (var enemy in GetSortedEnemies())
            {
                if (enemy == null || !board.ContainsPiece(enemy)) continue;

                if (enemy.TryGetComponent<BossEnemy>(out var boss))
                {
                    boss.ExecuteTurn(board);

                    yield return WaitForAttackAnimationOrDelay();

                    continue;
                }

                if (!TryResolveEnemyTarget(enemy, out var target)) continue;

                ExecuteEnemyActionAsCommand(enemy, target);

                yield return WaitForAttackAnimationOrDelay();
            }

            IEnumerator WaitForAttackAnimationOrDelay()
            {
                yield return null; // let this frame's Play*() calls register on the gate first

                float safetyTimeout = 3f, t = 0f;
                while (PieceMotionController.ActiveAnimationCount > 0 && t < safetyTimeout)
                {
                    t += Time.deltaTime;
                    yield return null;
                }

                if (t >= safetyTimeout)
                    Debug.LogWarning("[TurnManager] Animation gate timeout — possible leaked animation.");

                float minPacing = enemyMoveDelay * JuiceSettings.EnemyPacingMultiplier;
                if (minPacing > 0f)
                    yield return new WaitForSeconds(minPacing);
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
            
            NotifyAllPieceRuntimes_EndEnemyTurn();
            SetPhase(TurnPhase.Cleanup);

            float scaledPostEnemyTurnPause = postEnemyTurnPause * JuiceSettings.EnemyPacingMultiplier;
            if (scaledPostEnemyTurnPause > 0f)
                yield return new WaitForSeconds(scaledPostEnemyTurnPause);

            EnsureEncounterRunnerBound();
            if (encounterRunner != null && encounterRunner.IsVictoryReady(board))
            {
                PlayerWon();
                yield break;
            }

            board.ClearHighlights();
            BeginSpellPhase();
        }
    }
}
