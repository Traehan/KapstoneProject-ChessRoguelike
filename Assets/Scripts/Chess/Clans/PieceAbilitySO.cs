// Assets/Scripts/Chess/Abilities/PieceAbilitySO.cs
using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    /// <summary>
    /// Base class for per-piece, instance-scoped abilities (innate or upgrades with behavior).
    /// Mirror of clan AbilitySO, but scoped to a single Piece instance.
    /// </summary>
    
    public abstract class PieceAbilitySO : ScriptableObject
    {
        [Header("Meta")]
        [Tooltip("Shown in UI / tooltips.")] public string displayName;
        [TextArea] public string description;

        /// <summary>Lightweight context passed to hooks.</summary>
        public readonly struct PieceCtx
        {
            public readonly Piece piece;
            public readonly ChessBoard board;
            public readonly TurnManager tm;

            public PieceCtx(Piece piece, ChessBoard board, TurnManager tm)
            {
                this.piece = piece; this.board = board; this.tm = tm;
            }
        }

        /// <summary>Attack calc context used for pre/post hooks.</summary>
        public class AttackCtx
        {
            public Piece attacker;
            public Piece defender;
            public int baseDamage;          // starting damage before modifiers
            public int damageDelta;         // add your modifications here
            public bool bypassFortify;      // set true to ignore fortify reduction for this hit

            public AttackCtx(Piece a, Piece d, int baseDamage)
            {
                attacker = a; defender = d; this.baseDamage = baseDamage;
                damageDelta = 0; bypassFortify = false;
            }
        }

        // ===== Lifecycle hooks (override as needed) =====
        public virtual void OnSpawn(PieceCtx ctx) { }
        public virtual void OnBeginPlayerTurn(PieceCtx ctx) { }
        public virtual void OnEndPlayerTurn(PieceCtx ctx) { }
        public virtual void OnPieceMoved(PieceCtx ctx, Vector2Int from, Vector2Int to) { }
        public virtual void OnUndo(PieceCtx ctx) { }

        /// <summary>Chance to modify damage / flags before ResolveCombat applies reductions.</summary>
        public virtual void OnAttackPreCalc(PieceCtx ctx, AttackCtx atk) { }

        /// <summary>Called after HP is applied in ResolveCombat.</summary>
        public virtual void OnAttackResolved(PieceCtx ctx, AttackCtx atk) { }

        /// <summary>
        /// Optional: contribute hint tiles (auras, zones). Return true if you added any.
        /// </summary>
        public virtual bool TryGetHintTiles(PieceCtx ctx, List<Vector2Int> outTiles, out Color color)
        {
            color = default;
            return false;
        }
    }
}
