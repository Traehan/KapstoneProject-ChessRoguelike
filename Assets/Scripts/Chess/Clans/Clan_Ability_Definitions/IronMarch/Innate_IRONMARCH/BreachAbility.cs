using UnityEngine;

namespace Chess
{
    /// <summary>
    /// Siege Breaker's "Breach": on this piece's attack, before damage is calculated, reduce the
    /// target's Shield (the enemy-side status; see ShieldStatusUtility, not Fortify) by
    /// shieldReduction, minimum 0.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Chess/Piece Abilities/Breach",
        fileName = "PA_Breach")]
    public sealed class BreachAbility : PieceAbilitySO
    {
        [Header("Breach Settings")]
        [Tooltip("Shield stacks removed from the defender before damage is calculated.")]
        public int shieldReduction = 2;

        public override void OnAttackPreCalc(PieceCtx ctx, AttackCtx atk)
        {
            var piece = ctx.piece;
            if (piece == null || atk == null) return;

            // Both attacker's and defender's abilities receive this hook — only act when this
            // piece is the one doing the attacking.
            if (atk.attacker != piece) return;
            if (atk.defender == null) return;

            ShieldStatusUtility.RemoveShield(atk.defender, shieldReduction);
        }
    }
}
