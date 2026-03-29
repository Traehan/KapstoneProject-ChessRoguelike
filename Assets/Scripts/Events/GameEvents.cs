using UnityEngine;
using Card;

namespace Chess
{
    public static class GameEvents
    {
        // Turn flow
        public static System.Action<Team> OnTurnStarted;
        public static System.Action<Team> OnTurnEnded;
        public static System.Action<TurnPhase> OnPhaseChanged;
        public static System.Action<Piece> OnPieceStatsChanged;

        // Commands
        public static System.Action<IGameCommand> OnCommandExecuted;
        public static System.Action<IGameCommand> OnCommandUndone;
        public static System.Action<IGameCommand> OnCommandRedone;

        // Board / piece
        public static System.Action<Piece, Vector2Int> OnPieceSpawned; // piece, at
        public static System.Action<Piece, Vector2Int, Vector2Int, MoveReason> OnPieceMoved; // piece, from, to, reason

        // Capture/restore
        public static System.Action<Piece, Piece, Vector2Int> OnPieceCaptured; // victim, by, at
        public static System.Action<Piece, Vector2Int> OnPieceRestored;        // piece, at

        // Combat / health
        public static System.Action<AttackReport> OnAttackResolved;
        public static System.Action<Piece, int, Piece> OnPieceDamaged; // target, amount, source
        public static System.Action<Piece, int, Piece> OnPieceHealed;  // target, amount, source

        // Status
        public static System.Action<StatusChangeReport> OnStatusApplied;
        public static System.Action<StatusChangeReport> OnStatusRemoved;

        // Resources
        public static System.Action<int, int> OnAPChanged;   // current, max
        public static System.Action<int, int> OnManaChanged; // current, max

        // Cards
        public static System.Action<Card.Card, SpellCardPlayReport> OnSpellCardPlayed;
        public static System.Action<Card.Card, UnitCardPlayReport> OnUnitCardPlayed;

        public static System.Action<Card.Card> OnCardCreated;
        public static System.Action<Card.Card> OnCardDestroyed;
        public static System.Action<Card.Card> OnCardDrawn;
        public static System.Action<Card.Card> OnCardAddedToHand;
        public static System.Action<Card.Card> OnCardRemovedFromHand;
        public static System.Action<Card.Card> OnCardDiscarded;
        public static System.Action<Card.Card> OnCardExhausted;
        public static System.Action<Card.Card> OnCardReturnedToHand;
        public static System.Action<Card.Card> OnCardPlayed;

        // Spell flow
        public static System.Action<Card.Card> OnSpellCastStarted;
        public static System.Action<Card.Card, object> OnSpellTargetSelected;
        public static System.Action<Card.Card> OnSpellCastCancelled;
        public static System.Action<Card.Card, SpellCardPlayReport> OnSpellResolved;

        // Encounter flow
        public static System.Action OnEncounterWon;
        public static System.Action OnEncounterLost;

        // Music requests
        public static System.Action<SoundEventId> OnMusicRequested;

        public static void RaiseSpellCardPlayed(Card.Card card, SpellCardPlayReport report)
            => OnSpellCardPlayed?.Invoke(card, report);

        public static void RaiseUnitCardPlayed(Card.Card card, UnitCardPlayReport report)
            => OnUnitCardPlayed?.Invoke(card, report);
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

    public struct SpellCardPlayReport
    {
        public Card.Card card;
        public Team ownerTeam;
        public int manaSpent;
        public bool resolved;
        public Piece targetPiece;
        public Vector2Int targetCoord;
    }

    public struct UnitCardPlayReport
    {
        public Card.Card card;
        public Team ownerTeam;
        public int manaSpent;
        public bool resolved;
        public Piece spawnedPiece;
        public PieceDefinition summonedDefinition;
        public Vector2Int coord;
    }

    public struct StatusChangeReport
    {
        public Piece piece;
        public StatusId statusId;
        public int amountChanged;
        public int previousStacks;
        public int newStacks;
        public Piece source;
    }
}