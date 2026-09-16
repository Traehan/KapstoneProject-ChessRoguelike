using UnityEngine;
using Chess;

namespace Card
{
    [CreateAssetMenu(menuName = "Cards/Spell Effects/Add Fortify", fileName = "AddFortifySpellEffect")]
    public class AddFortifySpellEffectSO : SpellEffectSO
    {
        [Min(1)] public int fortifyAmount = 2;
        [Min(1)] public int maxStacks = 5;

        public override bool Resolve(SpellContext context)
        {
            if (context == null)
                return false;

            var target = context.TargetPiece;
            if (target == null)
                return false;

            if (target.Team != context.CasterTeam)
                return false;

            context.SetState("AddFortify_Target", target);
            context.SetState("AddFortify_PreviousStacks", FortifyStatusUtility.GetFortify(target));

            FortifyStatusUtility.AddFortify(target, fortifyAmount, maxStacks);
            GameEvents.OnPieceStatsChanged?.Invoke(target);

            return true;
        }

        public override void Undo(SpellContext context)
        {
            if (context == null)
                return;

            if (!context.TryGetState<Piece>("AddFortify_Target", out var target) || target == null)
                return;

            if (!context.TryGetState<int>("AddFortify_PreviousStacks", out var previousStacks))
                return;

            FortifyStatusUtility.SetFortify(target, previousStacks);
            GameEvents.OnPieceStatsChanged?.Invoke(target);
        }
    }
}