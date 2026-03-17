using UnityEngine;
using Chess;

namespace Card
{
    [CreateAssetMenu(menuName = "Cards/Spell Effects/Blood Court/Attack Buff + Next Spell Mana On Kill", fileName = "FX_AttackBuffNextSpellManaOnKill")]
    public class ApplyAttackBuffAndNextSpellManaOnKillEffectSo : SpellEffectSO
    {
        [Min(0)] public int attackBonus = 5;
        [Min(0)] public int nextSpellManaBonus = 2;

        string BuildStateKey(SpellContext context)
        {
            string cardId = context != null && context.Card != null ? context.Card.RuntimeId : "unknown";
            return $"AttackBuffNextSpellManaTracker_{cardId}";
        }

        public override bool Resolve(SpellContext context)
        {
            if (context == null || context.TurnManager == null)
                return false;

            Piece target = context.TargetPiece;
            if (target == null)
                return false;

            if (target.Team != context.CasterTeam)
                return false;

            var tracker = target.gameObject.AddComponent<NextSpellManaOnKillTracker>();
            tracker.Init(target, context.TurnManager, attackBonus, nextSpellManaBonus);

            context.SetState(BuildStateKey(context), tracker);
            return true;
        }

        public override void Undo(SpellContext context)
        {
            if (context == null)
                return;

            if (context.TryGetState(BuildStateKey(context), out NextSpellManaOnKillTracker tracker) && tracker != null)
                tracker.UndoTracker();
        }
    }
}