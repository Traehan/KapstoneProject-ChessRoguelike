using System.Collections.Generic;
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

        // Prevent the same dead unit from feeding the queen multiple times.
        readonly HashSet<int> _fedVictimIds = new();

        public override void OnBeginPlayerTurn(ClanRuntime ctx)
        {
            // Optional safety clear between turns if you want.
            // Usually not required, but harmless for normal play.
        }

        public override void OnAttackResolved(ClanRuntime ctx, Piece attacker, Piece defender, int dmgToDef, int dmgToAtk)
        {
            if (ctx == null || ctx.queen == null) return;

            // Combat may report the death here BEFORE the board removes the piece.
            if (attacker != null && attacker.currentHP <= 0)
                TryFeedOnAllyDeath(ctx, attacker);

            if (defender != null && defender.currentHP <= 0)
                TryFeedOnAllyDeath(ctx, defender);
        }

        public override void OnPieceCaptured(ClanRuntime ctx, Piece victim, Piece by, Vector2Int at)
        {
            if (ctx == null || ctx.queen == null) return;
            TryFeedOnAllyDeath(ctx, victim);
        }

        void TryFeedOnAllyDeath(ClanRuntime ctx, Piece dead)
        {
            if (dead == null) return;

            // Only allied pieces, not the queen herself.
            if (dead.Team != ctx.playerTeam) return;
            if (dead == ctx.queen) return;

            int victimId = dead.GetInstanceID();

            // Already counted this death.
            if (_fedVictimIds.Contains(victimId))
                return;

            _fedVictimIds.Add(victimId);

            ApplyQueenBuff(ctx.queen, gainMaxHp, gainCurrentHp, gainAttack);
            Debug.Log($"[BloodCourt_QueenFeast] Queen fed once on {dead.name} ({victimId}).");
        }

        public void ForgetVictim(Piece piece)
        {
            if (piece == null) return;
            _fedVictimIds.Remove(piece.GetInstanceID());
        }

        static void ApplyQueenBuff(Piece queen, int addMaxHp, int addCurHp, int addAtk)
        {
            if (queen == null) return;

            var rt = queen.GetComponent<PieceRuntime>();

            if (rt != null)
            {
                rt.MaxHP += addMaxHp;
                rt.CurrentHP = Mathf.Min(rt.CurrentHP + addCurHp, rt.MaxHP);
                rt.Attack += addAtk;

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