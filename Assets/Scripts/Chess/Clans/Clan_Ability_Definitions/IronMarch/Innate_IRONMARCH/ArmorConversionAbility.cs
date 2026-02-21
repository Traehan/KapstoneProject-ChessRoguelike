// Assets/Scripts/Chess/Abilities/ArmorConversionAbility.cs
using UnityEngine;

namespace Chess
{
    [CreateAssetMenu(
        menuName = "Chess/Piece Abilities/Armor Conversion",
        fileName = "PA_ArmorConversion")]
    public sealed class ArmorConversionAbility : PieceAbilitySO, IDisplayStatModifier
    {
        [Header("Armor Conversion Settings")]
        [Tooltip("How much attack bonus per Fortify stack.")]
        public int bonusPerFortify = 2;

        // UI display: show bonus ATK all the time based on current fortify stacks
        public int GetDisplayedAttackBonus(PieceCtx ctx)
        {
            var piece = ctx.piece;
            if (piece == null) return 0;

            int stacks = Mathf.Max(0, piece.fortifyStacks);
            return stacks * bonusPerFortify;
        }

        // Combat: apply same bonus when THIS piece attacks (does not consume stacks)
        public override void OnAttackPreCalc(PieceCtx ctx, AttackCtx atk)
        {
            var piece = ctx.piece;
            if (piece == null) return;

            if (atk.attacker != piece) return;

            int stacks = Mathf.Max(0, piece.fortifyStacks);
            if (stacks <= 0) return;

            atk.damageDelta += stacks * bonusPerFortify;
        }
    }
}