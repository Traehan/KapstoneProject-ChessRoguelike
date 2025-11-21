// Assets/Scripts/Chess/Abilities/ArmorConversionAbility.cs
using UnityEngine;

namespace Chess
{
    [CreateAssetMenu(
        menuName = "Chess/Piece Abilities/Armor Conversion",
        fileName = "PA_ArmorConversion")]
    public sealed class ArmorConversionAbility : PieceAbilitySO
    {
        [Header("Armor Conversion Settings")]
        [Tooltip("How much attack bonus per Fortify stack.")]
        public int bonusPerFortify = 1;

        public override void OnAttackPreCalc(PieceCtx ctx, AttackCtx atk)
        {
            var piece = ctx.piece;
            if (piece == null) return;

            // Only modify damage when THIS piece is the attacker
            if (atk.attacker != piece) return;

            int stacks = Mathf.Max(0, piece.fortifyStacks);
            if (stacks <= 0) return;

            int bonus = stacks * bonusPerFortify;
            atk.damageDelta += bonus;
        }
    }
}