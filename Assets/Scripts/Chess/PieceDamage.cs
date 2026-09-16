using UnityEngine;

namespace Chess
{
    /// <summary>
    /// Single entry point for applying damage/healing to a Piece's HP. Centralizes Fortify
    /// absorption, HP clamping, and the GameEvents.OnPieceDamaged/OnPieceHealed/OnPieceStatsChanged
    /// notifications so every damage source (melee, splash, bleed, spells) behaves consistently
    /// and any HP UI stays in sync with actual combat state.
    /// </summary>
    public static class PieceDamage
    {
        public readonly struct Result
        {
            public readonly int amountDealt;    // actual HP lost, after Fortify absorption
            public readonly int amountAbsorbed; // portion blocked by Fortify
            public readonly bool died;          // target.currentHP <= 0 after this call

            public Result(int amountDealt, int amountAbsorbed, bool died)
            {
                this.amountDealt = amountDealt;
                this.amountAbsorbed = amountAbsorbed;
                this.died = died;
            }
        }

        /// <summary>
        /// Applies incoming damage to a piece: runs it through Fortify then Shield (unless bypassed),
        /// subtracts HP, and fires the standard damage events. Does NOT capture the piece on
        /// death — callers decide when/how (immediate capture vs. a death animation first).
        /// Fortify and Shield are separate statuses (see FortifyStatusUtility / ShieldStatusUtility)
        /// but no single piece is expected to carry both today, so absorption order between them
        /// is not gameplay-significant; bypassFortify bypasses both, matching how "true damage"
        /// (e.g. Bleed ticks) is already expected to ignore all shielding.
        /// </summary>
        public static Result Apply(Piece target, int amount, Piece source = null, bool bypassFortify = false)
        {
            if (target == null || amount <= 0)
                return new Result(0, 0, target != null && target.currentHP <= 0);

            int afterFortify = FortifyStatusUtility.AbsorbDamage(target, amount, bypassFortify, source);
            int reduced = ShieldStatusUtility.AbsorbDamage(target, afterFortify, bypassFortify, source);
            int absorbed = amount - reduced;

            if (reduced > 0)
            {
                target.currentHP -= reduced;
                GameEvents.OnPieceDamaged?.Invoke(target, reduced, source);
                GameEvents.OnPieceStatsChanged?.Invoke(target);
                target.GetComponent<PieceRuntime>()?.Notify_DamageReceived(reduced, source);
            }

            return new Result(reduced, absorbed, target.currentHP <= 0);
        }

        /// <summary>
        /// Heals a piece, clamped to its current MaxHP, and fires the standard heal events.
        /// Returns the actual amount healed (may be less than requested near/at max HP).
        /// </summary>
        public static int Heal(Piece target, int amount, Piece source = null)
        {
            if (target == null || amount <= 0) return 0;

            int before = target.currentHP;
            target.currentHP = Mathf.Min(target.currentHP + amount, target.maxHP);
            int healed = target.currentHP - before;

            if (healed > 0)
            {
                GameEvents.OnPieceHealed?.Invoke(target, healed, source);
                GameEvents.OnPieceStatsChanged?.Invoke(target);
            }

            return healed;
        }
    }
}
