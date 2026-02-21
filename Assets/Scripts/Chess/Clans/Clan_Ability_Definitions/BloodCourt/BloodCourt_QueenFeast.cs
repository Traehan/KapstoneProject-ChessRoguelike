using UnityEngine;

namespace Chess
{
    [CreateAssetMenu(menuName = "Chess/Clans/Abilities/Blood Court/Queen - Feast of the Fallen", fileName = "BloodCourt_QueenFeast")]
    public sealed class BloodCourt_QueenFeast : AbilitySO
    {
        [Header("Scaling")]
        public int gainMaxHp = 2;
        public int gainCurrentHp = 2;
        public int gainAttack = 2;

        public override void OnAttackResolved(ClanRuntime ctx, Piece attacker, Piece defender, int dmgToDef, int dmgToAtk)
        {
            if (ctx == null || ctx.queen == null) return;

            // ResolveCombat fires this BEFORE pieces get removed, so HP<=0 is the reliable “died” signal.
            if (attacker != null && attacker.currentHP <= 0)
                TryFeedOnAllyDeath(ctx, attacker);

            if (defender != null && defender.currentHP <= 0)
                TryFeedOnAllyDeath(ctx, defender);
        }

        void TryFeedOnAllyDeath(ClanRuntime ctx, Piece dead)
        {
            if (dead == null) return;

            // ONLY allied pieces (and NOT the queen)
            if (dead.Team != ctx.playerTeam) return;
            if (dead == ctx.queen) return;

            ApplyQueenBuff(ctx.queen, gainMaxHp, gainCurrentHp, gainAttack);
        }

        static void ApplyQueenBuff(Piece queen, int addMaxHp, int addCurHp, int addAtk)
        {
            if (queen == null) return;

            // Prefer PieceRuntime stats if present (since your combat reads runtime Attack sometimes)
            var rt = queen.GetComponent<PieceRuntime>();

            if (rt != null)
            {
                rt.MaxHP += addMaxHp;
                rt.CurrentHP = Mathf.Min(rt.CurrentHP + addCurHp, rt.MaxHP);
                rt.Attack += addAtk;

                // Keep Piece fields in sync (your ResolveCombat subtracts Piece.currentHP)
                queen.maxHP = rt.MaxHP;
                queen.currentHP = rt.CurrentHP;
                queen.attack = rt.Attack;
            }
            else
            {
                queen.maxHP += addMaxHp;
                queen.currentHP = Mathf.Min(queen.currentHP + addCurHp, queen.maxHP);
                queen.attack += addAtk;
            }

            GameEvents.OnPieceStatsChanged?.Invoke(queen);
        }
    }
}