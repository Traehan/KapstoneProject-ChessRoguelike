using UnityEngine;

namespace Chess
{
    public static class GameEvents
    {
        // Turn flow
        public static System.Action<Team> OnTurnStarted;
        public static System.Action<Team> OnTurnEnded;
        public static System.Action<TurnPhase> OnPhaseChanged;

        // Commands
        public static System.Action<IGameCommand> OnCommandExecuted;
        public static System.Action<IGameCommand> OnCommandUndone;
        public static System.Action<IGameCommand> OnCommandRedone;

        // Board / piece
        public static System.Action<Piece, Vector2Int> OnPieceSpawned; // piece, at

        // NOTE: reason is critical so listeners can avoid doing "normal move" logic on undo moves.
        public static System.Action<Piece, Vector2Int, Vector2Int, MoveReason> OnPieceMoved; // piece, from, to, reason

        // Capture/restore
        public static System.Action<Piece, Piece, Vector2Int> OnPieceCaptured; // victim, by, at
        public static System.Action<Piece, Vector2Int> OnPieceRestored;        // piece, at

        // Combat / health
        public static System.Action<AttackReport> OnAttackResolved;
        public static System.Action<Piece, int, Piece> OnPieceDamaged; // target, amount, source
        public static System.Action<Piece, int, Piece> OnPieceHealed;  // target, amount, source

        // Resources
        public static System.Action<int, int> OnAPChanged; // current, max
    }

    public enum MoveReason
    {
        Normal,
        Undo,
        Redo,
        Forced
    }

    public struct AttackReport
    {
        public Piece attacker;
        public Piece defender;

        public int damageToDefender;
        public int damageToAttacker;

        public bool attackerDied;
        public bool defenderDied;

        // If later you expose it from ResolveCombat / ctx, set this correctly.
        public bool bypassedFortify;
        
        public Team attackerTeam;
        public bool isBossAttack;
        public MoveReason reason; // not needed now, but nice

    }
}