using UnityEngine;

namespace Chess
{
    [CreateAssetMenu(menuName = "Chess/Clans/Blood Court/Bleed On Attack", fileName = "BloodCourt_BleedOnAttack")]
    public sealed class BloodCourt_BleedOnAttack : AbilitySO
    {
        public int bleedPerAttack = 1;

        public override void OnAttackResolved(ClanRuntime ctx, Piece attacker, Piece defender, int dmgToDef, int dmgToAtk)
        {
            if (ctx == null || attacker == null || defender == null) return;

            // Only allied attacks apply bleed
            if (attacker.Team != ctx.playerTeam) return;

            // Only apply to enemies
            if (defender.Team == ctx.playerTeam) return;

            // Optional: if they died, don’t bother stacking bleed
            if (defender.currentHP <= 0) return;

            var bleed = defender.GetComponent<BleedStatus>();
            if (bleed == null) bleed = defender.gameObject.AddComponent<BleedStatus>();

            bleed.Add(bleedPerAttack);
        }
    }
}