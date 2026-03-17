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
        }
        
        public void BeginEncounterFromPreparation()
        {
            if (Phase != TurnPhase.Preparation) return;

            if (GameSession.I != null && deckManager != null)
                deckManager.InitializeBattleFromCardDefinitions(GameSession.I.CurrentRunDeck);
            
            EnsureQueenLeaderBound();
            BeginSpellPhase(); 
        }
        
        void BeginSpellPhase()
        {
            EnsureQueenLeaderBound();
            SetPhase(TurnPhase.SpellPhase);

            // Mana reset for this phase
            RefillManaForSpellPhase();

            // Show hand + draw to full
            deckManager?.DrawUpTo(4);

            var hand = FindObjectOfType<HandPanel>();
            if (hand != null)
            {
                hand.gameObject.SetActive(true);
                hand.RebuildHand();
            }
        }
        
        public void EndSpellPhaseButton()
        {
            if (Phase != TurnPhase.SpellPhase) return;

            // Hide hand while in PlayerTurn (movement/attacks)
            var hand = FindObjectOfType<HandPanel>();
            if (hand != null) hand.gameObject.SetActive(false);

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

            PaintAbilityHints();

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
            BeginSpellPhase();
        }
    }
}
