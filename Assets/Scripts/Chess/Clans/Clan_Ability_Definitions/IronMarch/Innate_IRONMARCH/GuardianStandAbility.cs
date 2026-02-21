// Assets/Scripts/Chess/Abilities/GuardianStandAbility.cs
using UnityEngine;

namespace Chess
{
    [CreateAssetMenu(
        menuName = "Chess/Piece Abilities/Guardian Stand",
        fileName = "PA_GuardianStand")]
    public sealed class GuardianStandAbility : PieceAbilitySO
    {
        [Header("Guardian Stand Settings")]
        [Tooltip("Fortify gained when this unit is attacked while in formation.")]
        public int fortifyGain = 1;

        [Tooltip("Maximum Fortify stacks this ability will grant.")]
        public int maxFortifyFromAbility = 3;

        public override void OnAttackResolved(PieceCtx ctx, AttackCtx atk)
        {
            var board = ctx.board;
            var self  = ctx.piece;

            if (board == null || self == null) return;

            // Only trigger when THIS unit is the defender
            if (atk.defender != self) return;

            // Require at least one adjacent ally (formation)
            if (!HasAdjacentAlly(board, self)) return;

            int current = self.fortifyStacks;
            int target  = Mathf.Min(current + fortifyGain, maxFortifyFromAbility);
            if (target <= current) return;

            self.fortifyStacks = target;
            GameEvents.OnPieceStatsChanged?.Invoke(self);

        }

        bool HasAdjacentAlly(ChessBoard board, Piece self)
        {
            Vector2Int origin = self.Coord;

            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                Vector2Int pos = origin + new Vector2Int(dx, dy);
                if (!board.InBounds(pos)) continue;
                if (!board.TryGetPiece(pos, out var p)) continue;
                if (p.Team == self.Team) return true;
            }

            return false;
        }
    }
}