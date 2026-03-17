using UnityEngine;
using Chess;

namespace Card
{
    [CreateAssetMenu(menuName = "Cards/Spell Effects/Blood Court/Grant Next Battle AP", fileName = "FX_GrantNextBattleAP")]
    public class GrantNextBattlePhaseAPEffectSO : SpellEffectSO
    {
        [Min(1)] public int amount = 1;

        public override bool Resolve(SpellContext context)
        {
            if (context == null || context.TurnManager == null)
                return false;

            context.TurnManager.GrantNextBattlePhaseAP(amount);
            return true;
        }

        public override void Undo(SpellContext context)
        {
            if (context == null || context.TurnManager == null)
                return;

            context.TurnManager.RemovePendingNextBattlePhaseAP(amount);
        }
    }
}