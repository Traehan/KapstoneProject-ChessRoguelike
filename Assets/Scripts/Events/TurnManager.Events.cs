using UnityEngine;

namespace Chess
{
    public partial class TurnManager
    {
        void OnEnable()
        {
            GameEvents.OnPieceMoved += HandlePieceMoved;
            GameEvents.OnAttackResolved += HandleAttackResolved;
            GameEvents.OnPieceCaptured += HandlePieceCaptured;
            GameEvents.OnPieceRestored += HandlePieceRestored;
            GameEvents.OnPieceDamaged += HandlePieceDamaged;

            GameEvents.OnCommandExecuted += HandleCommandExecuted;
            GameEvents.OnCommandUndone += HandleCommandUndone;
            GameEvents.OnCommandRedone += HandleCommandRedone;
        }

        void OnDisable()
        {
            GameEvents.OnPieceMoved -= HandlePieceMoved;
            GameEvents.OnAttackResolved -= HandleAttackResolved;
            GameEvents.OnPieceCaptured -= HandlePieceCaptured;
            GameEvents.OnPieceRestored -= HandlePieceRestored;
            GameEvents.OnPieceDamaged -= HandlePieceDamaged;

            GameEvents.OnCommandExecuted -= HandleCommandExecuted;
            GameEvents.OnCommandUndone -= HandleCommandUndone;
            GameEvents.OnCommandRedone -= HandleCommandRedone;
        }

        void HandlePieceMoved(Piece piece, Vector2Int from, Vector2Int to, MoveReason reason)
        {
            if (piece == null) return;

            bool isPlayerAction = (reason == MoveReason.Normal || reason == MoveReason.Redo);

            if (isPlayerAction && Phase == TurnPhase.PlayerTurn && piece.Team == playerTeam)
            {
                NotifyAbilitiesPieceMoved(piece);
                PaintAbilityHints();
            }

            if (Phase == TurnPhase.EnemyTurn && piece.Team == enemyTeam && reason == MoveReason.Forced)
            {
                if (EnemyReachedPlayerSide(piece))
                    LoseLifeAndDespawn(piece);
            }

            if (Phase == TurnPhase.PlayerTurn)
                RecomputeEnemyIntentsAndPaint();
        }

        void HandleAttackResolved(AttackReport r)
        {
            // ✅ CRITICAL: forward to clan abilities
            NotifyAbilitiesAttackResolved(r);

            RecomputeEnemyIntentsAndPaint();
            PaintAbilityHints();
        }

        void HandlePieceCaptured(Piece victim, Piece by, Vector2Int at)
        {
            EnsureEncounterRunnerBound();
            if (encounterRunner != null && encounterRunner.IsVictoryReady(board))
                PlayerWon();
        }

        void HandlePieceRestored(Piece piece, Vector2Int at)
        {
            RecomputeEnemyIntentsAndPaint();
            PaintAbilityHints();
        }

        void HandlePieceDamaged(Piece target, int amount, Piece source)
        {
            // optional: UI updates, SFX, etc.
            GameEvents.OnPieceStatsChanged?.Invoke(target);
        }

        void HandleCommandUndone(IGameCommand cmd) { }
        void HandleCommandRedone(IGameCommand cmd) { }
        void HandleCommandExecuted(IGameCommand cmd) { }
    }
}