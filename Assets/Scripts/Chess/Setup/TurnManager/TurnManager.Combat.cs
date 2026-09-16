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
            if (attacker == null || defender == null)
            {
                attackerDied = false;
                defenderDied = false;
                return;
            }

            int baseDmg = GetAttackValue(attacker);

            if (_ironMarchAura != null)
                baseDmg += _ironMarchAura.GetAttackBonusIfEligible(_clan, attacker);

            var ctx = new PieceAbilitySO.AttackCtx(attacker, defender, baseDmg);

            attacker.GetComponent<PieceRuntime>()?.CollectPreAttackModifiers(ctx);
            defender.GetComponent<PieceRuntime>()?.CollectPreAttackModifiers(ctx);

            int modifiedAtk = Mathf.Max(0, ctx.baseDamage + ctx.damageDelta);

            var hit = PieceDamage.Apply(defender, modifiedAtk, attacker, ctx.bypassFortify);
            defenderDied = hit.died;

            int retaliateStacks = RetaliateStatusUtility.GetRetaliate(defender);
            if (!defenderDied && retaliateStacks > 0)
            {
                int retaliationBase = GetAttackValue(defender) + GetRelicRetaliateBonusDamage(defender, attacker);
                PieceDamage.Apply(attacker, retaliationBase, defender, bypassFortify: false);
                RetaliateStatusUtility.RemoveRetaliate(defender, 1);
            }

            attackerDied = attacker.currentHP <= 0;

            attacker.GetComponent<PieceRuntime>()?.Notify_AttackResolved(ctx);
            defender.GetComponent<PieceRuntime>()?.Notify_AttackResolved(ctx);

            // IMPORTANT:
            // No OnAttackResolved here anymore.
            // Commands are now the single source of attack events.
        }

        public void ResolveBossAttack(Piece attacker, Piece defender, out bool defenderDied)
        {
            defenderDied = false;
            if (attacker == null || defender == null) return;

            int dmg = attacker.attack;
            if (_ironMarchAura != null)
                dmg += _ironMarchAura.GetAttackBonusIfEligible(_clan, attacker);

            var hit = PieceDamage.Apply(defender, dmg, attacker, bypassFortify: false);
            defenderDied = hit.died;

            int retaliateStacks = RetaliateStatusUtility.GetRetaliate(defender);
            if (!defenderDied && retaliateStacks > 0)
            {
                int retaliationBase = GetAttackValue(defender) + GetRelicRetaliateBonusDamage(defender, attacker);
                PieceDamage.Apply(attacker, retaliationBase, defender, bypassFortify: false);
                RetaliateStatusUtility.RemoveRetaliate(defender, 1);
            }

            // IMPORTANT:
            // No OnAttackResolved here anymore.
            // BossRayAttackCommand should raise it once.
        }
    }
}