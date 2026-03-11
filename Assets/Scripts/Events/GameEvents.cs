using UnityEngine;
using Card;

namespace Chess
{
    public static class GameEvents
    {
        // =========================
        // Turn flow
        // =========================
        public static System.Action<Team> OnTurnStarted;
        public static System.Action<Team> OnTurnEnded;
        public static System.Action<TurnPhase> OnPhaseChanged;

        // =========================
        // Piece / board / stats
        // =========================
        public static System.Action<Piece> OnPieceStatsChanged;
        public static System.Action<Piece, Vector2Int> OnPieceSpawned; // piece, at
        public static System.Action<Piece, Vector2Int, Vector2Int, MoveReason> OnPieceMoved; // piece, from, to, reason
        public static System.Action<Piece, Piece, Vector2Int> OnPieceCaptured; // victim, by, at
        public static System.Action<Piece, Vector2Int> OnPieceRestored; // piece, at

        // =========================
        // Combat / health
        // =========================
        public static System.Action<AttackReport> OnAttackResolved;
        public static System.Action<Piece, int, Piece> OnPieceDamaged; // target, amount, source
        public static System.Action<Piece, int, Piece> OnPieceHealed;  // target, amount, source

        // =========================
        // Commands
        // =========================
        public static System.Action<IGameCommand> OnCommandExecuted;
        public static System.Action<IGameCommand> OnCommandUndone;
        public static System.Action<IGameCommand> OnCommandRedone;

        // =========================
        // Resources
        // =========================
        public static System.Action<int, int> OnAPChanged;   // current, max
        public static System.Action<int, int> OnManaChanged; // current, max

        // =========================
        // Card lifecycle
        // =========================
        public static System.Action<Card.Card> OnCardCreated;        // runtime card created
        public static System.Action<Card.Card> OnCardDestroyed;      // runtime card removed permanently if needed
        public static System.Action<Card.Card> OnCardDrawn;          // moved into hand
        public static System.Action<Card.Card> OnCardAddedToHand;    // explicit hand add
        public static System.Action<Card.Card> OnCardRemovedFromHand;// explicit hand removal
        public static System.Action<Card.Card> OnCardDiscarded;      // sent to discard
        public static System.Action<Card.Card> OnCardExhausted;      // future-proof
        public static System.Action<Card.Card> OnCardReturnedToHand; // undo / bounce / recovery
        public static System.Action<Card.Card> OnCardPlayed;         // any successful play

        // =========================
        // Specific card plays
        // =========================
        public static System.Action<Card.Card, UnitCardPlayReport> OnUnitCardPlayed;
        public static System.Action<Card.Card, SpellCardPlayReport> OnSpellCardPlayed;

        // =========================
        // Spell lifecycle / targeting
        // =========================
        public static System.Action<Card.Card> OnSpellCastStarted;   // selected/initiated
        public static System.Action<Card.Card> OnSpellCastCancelled; // cancelled before resolve
        public static System.Action<Card.Card, SpellCardPlayReport> OnSpellResolved;
        public static System.Action<Card.Card, object> OnSpellTargetSelected;

        // =========================
        // Helpers
        // =========================
        public static void RaiseCardPlayed(Card.Card card)
        {
            if (card == null) return;

            OnCardPlayed?.Invoke(card);

            if (card.IsUnitCard())
            {
                OnUnitCardPlayed?.Invoke(card, new UnitCardPlayReport
                {
                    card = card
                });
            }
            else if (card.IsSpellCard())
            {
                OnSpellCardPlayed?.Invoke(card, new SpellCardPlayReport
                {
                    card = card
                });
            }
        }
    }

    public enum MoveReason
    {
        Normal,
        Undo,
        Redo,
        Forced,
        SpellEffect
    }

    public struct AttackReport
    {
        public Piece attacker;
        public Piece defender;

        public int damageToDefender;
        public int damageToAttacker;

        public bool attackerDied;
        public bool defenderDied;

        public bool bypassedFortify;

        public Team attackerTeam;
        public bool isBossAttack;
        public MoveReason reason;
    }

    public struct UnitCardPlayReport
    {
        public Card.Card card;
        public PieceDefinition summonedDefinition;
        public Piece spawnedPiece;
        public Vector2Int coord;
        public Team ownerTeam;
        public int manaSpent;
    }

    public struct SpellCardPlayReport
    {
        public Card.Card card;
        public CardTargetingMode targetingMode;
        public object target;
        public Team casterTeam;
        public int manaSpent;
        public bool resolved;
    }
}