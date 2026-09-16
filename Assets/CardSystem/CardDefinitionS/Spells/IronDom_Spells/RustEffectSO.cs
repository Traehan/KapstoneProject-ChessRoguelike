using UnityEngine;
using Chess;

namespace Card
{
    /// <summary>
    /// Rust: target enemy piece loses shieldReduction Shield (the enemy-side status; see
    /// ShieldStatusUtility, not Fortify), minimum 0.
    /// </summary>
    [CreateAssetMenu(menuName = "Cards/Spell Effects/Rust", fileName = "RustEffect")]
    public class RustEffectSO : SpellEffectSO
    {
        [Min(1)] public int shieldReduction = 3;

        const string TargetKey = "Rust_Target";
        const string PrevKey = "Rust_PreviousShield";

        public override bool Resolve(SpellContext context)
        {
            if (context == null)
                return false;

            var target = context.TargetPiece;
            if (target == null)
                return false;

            if (target.Team == context.CasterTeam)
                return false;

            int previous = ShieldStatusUtility.GetShield(target);

            context.SetState(TargetKey, target);
            context.SetState(PrevKey, previous);

            ShieldStatusUtility.RemoveShield(target, shieldReduction);
            GameEvents.OnPieceStatsChanged?.Invoke(target);

            return true;
        }

        public override void Undo(SpellContext context)
        {
            if (context == null)
                return;

            if (!context.TryGetState<Piece>(TargetKey, out var target) || target == null)
                return;

            if (!context.TryGetState<int>(PrevKey, out var previous))
                return;

            ShieldStatusUtility.SetShield(target, previous);
            GameEvents.OnPieceStatsChanged?.Invoke(target);
        }
    }
}
