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
        public int bonusPerFortify = 3;

        public int GetDisplayedAttackBonus(PieceCtx ctx)
        {
            var piece = ctx.piece;
            if (piece == null) return 0;

            int stacks = FortifyStatusUtility.GetFortify(piece);
            return Mathf.Max(0, stacks) * bonusPerFortify;
        }

        public override void OnAttackPreCalc(PieceCtx ctx, AttackCtx atk)
        {
            var piece = ctx.piece;
            if (piece == null) return;
            if (atk.attacker != piece) return;

            int stacks = FortifyStatusUtility.GetFortify(piece);
            if (stacks <= 0) return;

            atk.damageDelta += stacks * bonusPerFortify;
        }
    }
}