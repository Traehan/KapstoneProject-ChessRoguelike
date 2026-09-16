using UnityEngine;

namespace Chess
{
    // Every Retaliate counter-hit from one of your pieces deals extra flat damage.
    [CreateAssetMenu(menuName = "Relics/Iron Dominion/Counterweight")]
    public class CounterweightRelicSO : RelicSO
    {
        [SerializeField, Min(0)] int bonusDamage = 1;

        public override int GetRetaliateBonusDamage(ClanRuntime ctx, Piece retaliator, Piece attacker)
        {
            if (ctx == null || retaliator == null || retaliator.Team != ctx.playerTeam) return 0;
            return bonusDamage;
        }
    }
}
