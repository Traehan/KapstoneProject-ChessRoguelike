using UnityEngine;
using Chess;

namespace Card
{
    [CreateAssetMenu(menuName = "Cards/Spell Effects/Add Retaliate", fileName = "AddRetaliateSpellEffect")]
    public class AddRetaliateSpellEffectSO : SpellEffectSO
    {
        [Min(1)] public int retaliateAmount = 1;
        [Min(1)] public int maxStacks = 3;

        public override bool Resolve(SpellContext context)
        {
            if (context == null)
                return false;

            var target = context.TargetPiece;
            if (target == null)
                return false;

            if (target.Team != context.CasterTeam)
                return false;

            int previousStacks = RetaliateStatusUtility.GetRetaliate(target);
            int nextStacks = Mathf.Min(previousStacks + retaliateAmount, maxStacks);

            context.SetState("AddRetaliate_Target", target);
            context.SetState("AddRetaliate_PreviousStacks", previousStacks);

            RetaliateStatusUtility.SetRetaliate(target, nextStacks);
            GameEvents.OnPieceStatsChanged?.Invoke(target);

            return true;
        }

        public override void Undo(SpellContext context)
        {
            if (context == null)
                return;

            if (!context.TryGetState<Piece>("AddRetaliate_Target", out var target) || target == null)
                return;

            if (!context.TryGetState<int>("AddRetaliate_PreviousStacks", out var previousStacks))
                return;

            RetaliateStatusUtility.SetRetaliate(target, previousStacks);
            GameEvents.OnPieceStatsChanged?.Invoke(target);
        }
    }
}