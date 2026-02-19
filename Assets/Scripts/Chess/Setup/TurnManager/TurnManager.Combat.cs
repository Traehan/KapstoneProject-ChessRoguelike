using UnityEngine;

namespace Chess
{
    public partial class TurnManager
    {
        
        int GetAttackValue(Piece p)
        {
            if (p == null) return 0;

            var rt = p.GetComponent<Chess.PieceRuntime>();
            if (rt != null) return rt.Attack;

            return p.attack;
        }

        public void ResolveCombat(Piece attacker, Piece defender, bool attackerIsPlayer,
            out bool attackerDied, out bool defenderDied)
        {
            int baseDmg = GetAttackValue(attacker);
            if (_ironMarchAura != null)
                baseDmg += _ironMarchAura.GetAttackBonusIfEligible(_clan, attacker);

            var ctx = new PieceAbilitySO.AttackCtx(attacker, defender, baseDmg);
            attacker.GetComponent<PieceRuntime>()?.CollectPreAttackModifiers(ctx);
            defender.GetComponent<PieceRuntime>()?.CollectPreAttackModifiers(ctx);

            int atk = ctx.bypassFortify ? ctx.baseDamage : Mathf.Max(0, ctx.baseDamage - defender.fortifyStacks);
            int ret = attackerIsPlayer ? 0 : Mathf.Max(0, defender.attack - attacker.fortifyStacks);

            defender.currentHP -= atk;
            attacker.currentHP -= ret;

            attackerDied = attacker.currentHP <= 0;
            defenderDied = defender.currentHP <= 0;

            attacker.GetComponent<PieceRuntime>()?.Notify_AttackResolved(ctx);
            defender.GetComponent<PieceRuntime>()?.Notify_AttackResolved(ctx);
        }

        public void ResolveBossAttack(Piece attacker, Piece defender, out bool defenderDied)
        {
            defenderDied = false;
            if (attacker == null || defender == null) return;

            int dmg = attacker.attack;
            if (_ironMarchAura != null)
                dmg += _ironMarchAura.GetAttackBonusIfEligible(_clan, attacker);

            defender.currentHP -= Mathf.Max(0, dmg - defender.fortifyStacks);
            defenderDied = defender.currentHP <= 0;
        }
    }
}