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

            // include damageDelta
            int modifiedAtk = Mathf.Max(0, ctx.baseDamage + ctx.damageDelta);

            // Apply Fortify unless bypassed
            int atkToDef = ctx.bypassFortify
                ? modifiedAtk
                : Mathf.Max(0, modifiedAtk - defender.fortifyStacks);

            int defToAtk = attackerIsPlayer ? 0 : Mathf.Max(0, defender.attack - attacker.fortifyStacks);

            // --- Apply damage ---
            if (atkToDef > 0)
                GameEvents.OnPieceDamaged?.Invoke(defender, atkToDef, attacker);

            if (defToAtk > 0)
                GameEvents.OnPieceDamaged?.Invoke(attacker, defToAtk, defender);

            defender.currentHP -= atkToDef;
            attacker.currentHP -= defToAtk;

            attackerDied = attacker.currentHP <= 0;
            defenderDied = defender.currentHP <= 0;

            attacker.GetComponent<PieceRuntime>()?.Notify_AttackResolved(ctx);
            defender.GetComponent<PieceRuntime>()?.Notify_AttackResolved(ctx);

            // ✅ CRITICAL: fire attack resolved event so clan abilities (Bleed) can react
            GameEvents.OnAttackResolved?.Invoke(new AttackReport
            {
                attacker = attacker,
                defender = defender,
                damageToDefender = atkToDef,
                damageToAttacker = defToAtk,
                attackerDied = attackerDied,
                defenderDied = defenderDied,
                bypassedFortify = ctx.bypassFortify,
                attackerTeam = attacker.Team,
                isBossAttack = false,
                reason = MoveReason.Forced // not used yet, safe default
            });
        }

        public void ResolveBossAttack(Piece attacker, Piece defender, out bool defenderDied)
        {
            defenderDied = false;
            if (attacker == null || defender == null) return;

            int dmg = attacker.attack;
            if (_ironMarchAura != null)
                dmg += _ironMarchAura.GetAttackBonusIfEligible(_clan, attacker);

            int final = Mathf.Max(0, dmg - defender.fortifyStacks);

            if (final > 0)
                GameEvents.OnPieceDamaged?.Invoke(defender, final, attacker);

            defender.currentHP -= final;
            defenderDied = defender.currentHP <= 0;

            // ✅ Also fire OnAttackResolved for boss attacks (optional but consistent)
            GameEvents.OnAttackResolved?.Invoke(new AttackReport
            {
                attacker = attacker,
                defender = defender,
                damageToDefender = final,
                damageToAttacker = 0,
                attackerDied = false,
                defenderDied = defenderDied,
                bypassedFortify = false,
                attackerTeam = attacker.Team,
                isBossAttack = true,
                reason = MoveReason.Forced
            });
        }
    }
}