using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    [CreateAssetMenu(menuName = "Chess/Piece Abilities/Piercing (Line Damage)", fileName = "PiercingAbility")]
    public sealed class PiercingAbility : PieceAbilitySO
    {
        [Header("Piercing Settings")]
        [Tooltip("Extra damage applied to each additional enemy hit.")]
        public int splashDamage = 0;
        [Tooltip("Max number of additional enemies hit behind the first target.")]
        public int maxPierceCount = 2;
        [Tooltip("Stops at the first allied piece even if enemies remain beyond.")]
        public bool stopAtAllies = true;

        // --- Called before ResolveCombat applies Fortify reductions ---
        public override void OnAttackResolved(PieceCtx ctx, AttackCtx atk)
        {
            var board = ctx.board;
            var attacker = ctx.piece;
            var defender = atk.defender;

            // Only apply if this piece was the attacker and successfully hit
            if (attacker == null || defender == null || board == null) return;
            if (atk.attacker != attacker) return;

            Vector2Int dir = defender.Coord - attacker.Coord;
            dir.Clamp(new Vector2Int(-1, -1), new Vector2Int(1, 1)); // normalize direction (-1/0/1 per axis)
            if (dir == Vector2Int.zero) return;

            // Trace forward along the same line
            Vector2Int cur = defender.Coord + dir;
            int pierced = 0;
            while (board.InBounds(cur) && pierced < maxPierceCount)
            {
                if (!board.TryGetPiece(cur, out var piece))
                {
                    cur += dir;
                    continue;
                }

                // Stop if we hit an ally
                if (piece.Team == attacker.Team && stopAtAllies)
                    break;

                // Damage enemy pieces
                if (piece.Team != attacker.Team)
                {
                    int dmg = Mathf.Max(1, atk.baseDamage + atk.damageDelta + splashDamage);
                    piece.currentHP -= dmg;
                    if (piece.currentHP <= 0)
                        board.CapturePiece(piece);

                    pierced++;
                }

                cur += dir;
                Debug.Log($"Piercing triggered on {ctx.piece.name}");
            }
        }
    }
}
