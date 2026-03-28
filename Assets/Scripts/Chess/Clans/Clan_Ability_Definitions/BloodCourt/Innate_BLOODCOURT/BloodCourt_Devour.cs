using UnityEngine;

namespace Chess
{
    [CreateAssetMenu(
        menuName = "Chess/Abilities/Piece/Blood Court/Devour",
        fileName = "BloodCourt_Devour")]
    public sealed class BloodCourt_Devour : PieceAbilitySO
    {
        [Header("Rewards")]
        [Min(0)] public int attackGain = 5;
        [Min(0)] public int apGain = 1;

        public override void OnAttackResolved(PieceCtx ctx, AttackCtx atk)
        {
            if (ctx.piece == null || ctx.tm == null || atk == null)
                return;

            // This passive only triggers when THIS unit is the attacker.
            if (atk.attacker != ctx.piece)
                return;

            if (atk.defender == null)
                return;

            // Must have killed the defender.
            if (atk.defender.currentHP > 0)
                return;

            // Defender must have been bleeding.
            var sc = atk.defender.GetComponent<StatusController>();
            if (sc == null)
                return;

            if (sc.GetStacks(StatusId.Bleed) <= 0)
                return;

            ApplyAttackGain(ctx, attackGain);

            if (apGain > 0 && ctx.tm.Phase == TurnPhase.PlayerTurn)
                ctx.tm.RefundAP(apGain);
        }

        void ApplyAttackGain(PieceCtx ctx, int amount)
        {
            if (amount <= 0 || ctx.piece == null)
                return;

            var runtime = ctx.piece.GetComponent<PieceRuntime>();
            if (runtime != null)
            {
                runtime.Attack += amount;
                ctx.piece.attack = runtime.Attack;
            }
            else
            {
                ctx.piece.attack += amount;
            }

            GameEvents.OnPieceStatsChanged?.Invoke(ctx.piece);
        }
    }
}