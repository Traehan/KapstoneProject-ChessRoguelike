using UnityEngine;
using Chess;

namespace Card
{
    [CreateAssetMenu(menuName = "Cards/Spell Effects/Reinforced Plating", fileName = "ReinforcedPlatingEffect")]
    public class ReinforcedPlatingEffectSO : SpellEffectSO
    {
        [Min(1)] public int maxStacks = 20;

        const string TargetKey = "ReinforcedPlating_Target";
        const string PrevKey = "ReinforcedPlating_PreviousFortify";

        public override bool Resolve(SpellContext context)
        {
            if (context == null)
                return false;

            var target = context.TargetPiece;
            if (target == null)
                return false;

            if (target.Team != context.CasterTeam)
                return false;

            int current = FortifyStatusUtility.GetFortify(target);
            if (current <= 0)
                return false;

            int next = Mathf.Min(current * 2, maxStacks);

            context.SetState(TargetKey, target);
            context.SetState(PrevKey, current);

            FortifyStatusUtility.SetFortify(target, next);
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

            FortifyStatusUtility.SetFortify(target, previous);
            GameEvents.OnPieceStatsChanged?.Invoke(target);
        }
    }
}