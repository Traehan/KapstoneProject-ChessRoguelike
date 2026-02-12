// Assets/Scripts/Chess/Abilities/SweepingStrikeAbility.cs
using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    [CreateAssetMenu(
        menuName = "Chess/Piece Abilities/Sweeping Strike",
        fileName = "PA_SweepingStrike")]
    public sealed class SweepingStrikeAbility : PieceAbilitySO
    {
        [Header("Sweeping Strike Settings")]
        [Tooltip("Damage dealt to enemies left/right of the main target.")]
        public int sideDamage = 1;

        public override void OnAttackResolved(PieceCtx ctx, AttackCtx atk)
        {
            var board    = ctx.board;
            var attacker = ctx.piece;
            var defender = atk.defender;

            // Only trigger when THIS piece is the attacker and there was a real target
            if (board == null || attacker == null || defender == null) return;
            if (atk.attacker != attacker) return;

            Vector2Int center = defender.Coord;

            // Left and right relative to board x-axis
            Vector2Int[] offsets =
            {
                new Vector2Int(-1, 0),
                new Vector2Int(+1, 0)
            };

            foreach (var off in offsets)
            {
                Vector2Int pos = center + off;
                if (!board.InBounds(pos)) continue;
                if (!board.TryGetPiece(pos, out var p)) continue;
                if (p.Team == attacker.Team) continue; // only hit enemies

                int dmg = Mathf.Max(0, sideDamage);
                if (dmg <= 0) continue;

                p.currentHP -= dmg;
                if (p.currentHP <= 0)
                {
                    board.CapturePiece(p);
                }
            }
        }
    }
}