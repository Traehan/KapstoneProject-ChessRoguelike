using UnityEngine;
using Card;

namespace Chess
{
    public partial class TurnManager
    {
        void OnEnable()
        {
            GameEvents.OnPieceMoved += HandlePieceMoved;

            GameEvents.OnAttackResolved -= HandleAttackResolved;
            GameEvents.OnAttackResolved += HandleAttackResolved;

            GameEvents.OnPieceCaptured += HandlePieceCaptured;
            GameEvents.OnPieceRestored += HandlePieceRestored;
            GameEvents.OnPieceDamaged += HandlePieceDamaged;

            GameEvents.OnCommandExecuted += HandleCommandExecuted;
            GameEvents.OnCommandUndone += HandleCommandUndone;
            GameEvents.OnCommandRedone += HandleCommandRedone;

            GameEvents.OnUnitCardPlayed += HandleUnitCardPlayed;
            GameEvents.OnSpellCardPlayed += HandleSpellCardPlayed;
            GameEvents.OnSpellResolved += HandleSpellResolved;
            GameEvents.OnCardDrawn += HandleCardDrawn;
            GameEvents.OnCardDiscarded += HandleCardDiscarded;
            GameEvents.OnCardReturnedToHand += HandleCardReturnedToHand;
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

            GameEvents.OnUnitCardPlayed -= HandleUnitCardPlayed;
            GameEvents.OnSpellCardPlayed -= HandleSpellCardPlayed;
            GameEvents.OnSpellResolved -= HandleSpellResolved;
            GameEvents.OnCardDrawn -= HandleCardDrawn;
            GameEvents.OnCardDiscarded -= HandleCardDiscarded;
            GameEvents.OnCardReturnedToHand -= HandleCardReturnedToHand;
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
            NotifyAbilitiesAttackResolved(r);
            RecomputeEnemyIntentsAndPaint();
            PaintAbilityHints();

            Debug.Log($"OnAttackResolved fired: attacker={r.attacker?.name} defender={r.defender?.name}");
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
            GameEvents.OnPieceStatsChanged?.Invoke(target);
        }

        void HandleCommandUndone(IGameCommand cmd) { }
        void HandleCommandRedone(IGameCommand cmd) { }
        void HandleCommandExecuted(IGameCommand cmd) { }

        void HandleCardDrawn(Card.Card card)
        {
            // Great place later for relics / passives / UI pings
            Debug.Log($"[TurnManager] Card drawn: {card?.Title}");
        }

        void HandleCardDiscarded(Card.Card card)
        {
            Debug.Log($"[TurnManager] Card discarded: {card?.Title}");
        }

        void HandleCardReturnedToHand(Card.Card card)
        {
            Debug.Log($"[TurnManager] Card returned to hand: {card?.Title}");
        }

        void HandleUnitCardPlayed(Card.Card card, UnitCardPlayReport report)
        {
            if (card == null) return;

            Debug.Log($"[TurnManager] Unit card played: {card.Title} at {report.coord}");

            // Good future hook:
            // - clan passives reacting to summons
            // - relics that care about played units
            // - achievements / analytics
        }

        void HandleSpellCardPlayed(Card.Card card, SpellCardPlayReport report)
        {
            if (card == null) return;

            Debug.Log($"[TurnManager] Spell card played: {card.Title}");

            // Good future hook:
            // - clan passives reacting to spell cast
            // - "first spell each turn" effects
            // - VFX/SFX routing
        }

        void HandleSpellResolved(Card.Card card, SpellCardPlayReport report)
        {
            if (card == null) return;

            Debug.Log($"[TurnManager] Spell resolved: {card.Title}, resolved={report.resolved}");

            PaintAbilityHints();
            RecomputeEnemyIntentsAndPaint();
        }
    }
}