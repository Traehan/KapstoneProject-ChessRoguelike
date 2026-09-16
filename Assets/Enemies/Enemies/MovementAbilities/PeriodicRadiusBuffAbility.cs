using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    /// <summary>
    /// Every N enemy turns, buffs allied pieces within a radius (Chebyshev distance) of this
    /// piece. Demonstrates OnEndEnemyTurn as a periodic trigger for enemy support archetypes.
    /// Per-piece turn counters are keyed by instance ID (same pattern as
    /// BloodCourt_QueenFeast._fedVictimIds) so one shared SO asset works correctly across
    /// multiple enemy instances of this variant in the same encounter.
    /// </summary>
    [CreateAssetMenu(menuName = "Chess/Piece Abilities/Periodic Radius Buff", fileName = "PA_PeriodicRadiusBuff")]
    public sealed class PeriodicRadiusBuffAbility : PieceAbilitySO
    {
        [Header("Timing")]
        [Min(1)] public int turnInterval = 3;

        [Header("Area")]
        [Min(0)] public int radius = 1;
        public bool includeSelf = false;

        [Header("Buff")]
        public int addMaxHP = 0;
        public int addAttack = 1;

        readonly Dictionary<int, int> _turnsSinceLastPulse = new();

        public override void OnEndEnemyTurn(PieceCtx ctx)
        {
            var piece = ctx.piece;
            if (piece == null) return;

            int id = piece.GetInstanceID();
            _turnsSinceLastPulse.TryGetValue(id, out int count);
            count++;

            if (count < turnInterval)
            {
                _turnsSinceLastPulse[id] = count;
                return;
            }

            _turnsSinceLastPulse[id] = 0;
            PulseBuff(ctx);
        }

        void PulseBuff(PieceCtx ctx)
        {
            var board = ctx.board;
            var piece = ctx.piece;
            if (board == null || piece == null) return;

            Vector2Int origin = piece.Coord;

            for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -radius; dy <= radius; dy++)
            {
                if (dx == 0 && dy == 0 && !includeSelf) continue;

                var pos = origin + new Vector2Int(dx, dy);
                if (!board.InBounds(pos)) continue;
                if (!board.TryGetPiece(pos, out var ally)) continue;
                if (ally.Team != piece.Team) continue;

                var rt = ally.GetComponent<PieceRuntime>();
                if (rt == null) continue;

                if (addMaxHP != 0)
                {
                    rt.MaxHP += addMaxHP;
                    if (addMaxHP > 0) PieceDamage.Heal(ally, addMaxHP, piece);
                }
                if (addAttack != 0)
                    rt.Attack += addAttack;

                GameEvents.OnPieceStatsChanged?.Invoke(ally);
            }
        }

        public override bool TryGetHintTiles(PieceCtx ctx, List<Vector2Int> outTiles, out Color color)
        {
            color = new Color(1f, 0.85f, 0.3f, 0.25f);
            var board = ctx.board;
            var piece = ctx.piece;
            if (board == null || piece == null) return false;

            Vector2Int origin = piece.Coord;

            for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -radius; dy <= radius; dy++)
            {
                if (dx == 0 && dy == 0 && !includeSelf) continue;

                var pos = origin + new Vector2Int(dx, dy);
                if (!board.InBounds(pos)) continue;
                outTiles.Add(pos);
            }

            return outTiles.Count > 0;
        }
    }
}
