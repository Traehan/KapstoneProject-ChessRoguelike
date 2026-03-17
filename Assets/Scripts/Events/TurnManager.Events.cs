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

            GameEvents.OnSpellCardPlayed += HandleSpellCardPlayed;
            GameEvents.OnUnitCardPlayed += HandleUnitCardPlayed;

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

            GameEvents.OnSpellCardPlayed -= HandleSpellCardPlayed;
            GameEvents.OnUnitCardPlayed -= HandleUnitCardPlayed;

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

                var runtime = piece.GetComponent<PieceRuntime>();
                if (runtime != null)
                    runtime.Notify_PieceMoved(from, to);

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
            if (_abilities != null)
            {
                foreach (var a in _abilities)
                    a?.OnAttackResolved(_clan, r.attacker, r.defender, r.damageToDefender, r.damageToAttacker);
            }

            RecomputeEnemyIntentsAndPaint();
            PaintAbilityHints();
        }

        void HandlePieceCaptured(Piece victim, Piece by, Vector2Int at)
        {
            if (_abilities != null)
            {
                foreach (var a in _abilities)
                    a?.OnPieceCaptured(_clan, victim, by, at);
            }

            EnsureEncounterRunnerBound();
            if (encounterRunner != null && encounterRunner.IsVictoryReady(board))
                PlayerWon();
        }

        void HandlePieceRestored(Piece piece, Vector2Int at)
        {
            if (_abilities != null)
            {
                foreach (var a in _abilities)
                {
                    if (a is BloodCourt_QueenFeast feast)
                        feast.ForgetVictim(piece);
                }
            }

            RecomputeEnemyIntentsAndPaint();
            PaintAbilityHints();
        }

        void HandlePieceDamaged(Piece target, int amount, Piece source)
        {
        }

        void HandleSpellCardPlayed(Card.Card card, SpellCardPlayReport report)
        {
            if (board == null) return;

            foreach (var piece in board.GetAllPieces())
            {
                if (piece == null) continue;
                if (piece.Team != playerTeam) continue;

                var runtime = piece.GetComponent<PieceRuntime>();
                if (runtime == null) continue;

                runtime.Notify_SpellCardPlayed(card, report);
            }

            PaintAbilityHints();
        }

        void HandleUnitCardPlayed(Card.Card card, UnitCardPlayReport report)
        {
            if (board == null) return;

            foreach (var piece in board.GetAllPieces())
            {
                if (piece == null) continue;
                if (piece.Team != playerTeam) continue;

                var runtime = piece.GetComponent<PieceRuntime>();
                if (runtime == null) continue;

                runtime.Notify_UnitCardPlayed(card, report);
            }

            PaintAbilityHints();
        }

        void HandleCommandUndone(IGameCommand cmd) { }
        void HandleCommandRedone(IGameCommand cmd) { }
        void HandleCommandExecuted(IGameCommand cmd) { }
    }
}