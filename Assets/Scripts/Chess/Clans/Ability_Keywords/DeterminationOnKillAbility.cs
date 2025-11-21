// Assets/Scripts/Chess/Abilities/DeterminationOnKillAbility.cs
using UnityEngine;

namespace Chess
{
    [CreateAssetMenu(
        menuName = "Chess/Piece Abilities/Determination (Gain AP on Kill)",
        fileName = "PA_DeterminationOnKill")]
    public sealed class DeterminationOnKillAbility : PieceAbilitySO
    {
        [Header("Determination Settings")]
        [Tooltip("How many times this can trigger per player turn.")]
        public int maxTriggersPerTurn = 2;

        private int _triggersThisTurn = 0;

        public override void OnBeginPlayerTurn(PieceCtx ctx)
        {
            // Reset per-turn counter
            _triggersThisTurn = 0;
        }

        public override void OnAttackResolved(PieceCtx ctx, AttackCtx atk)
        {
            var tm       = ctx.tm;
            var attacker = ctx.piece;
            var defender = atk.defender;

            if (tm == null || attacker == null || defender == null) return;

            // Only care when THIS piece is the attacker
            if (atk.attacker != attacker) return;

            // Only during player turn and for the player's team
            if (!tm.IsPlayerTurn || attacker.Team != tm.PlayerTeam) return;

            // Must have a kill
            if (defender.currentHP > 0) return;

            if (_triggersThisTurn >= maxTriggersPerTurn) return;

            // Hack: TrySpendAP(-1) = "gain" 1 AP (since it subtracts negative).
            // NOTE: This can push AP above the normal per-turn cap, which is a fun
            // power fantasy for Determination. If you want to clamp it later,
            // we can add a dedicated GainAP() method on TurnManager.
            bool success = tm.TrySpendAP(-1);
            if (success)
            {
                _triggersThisTurn++;
#if UNITY_EDITOR
                Debug.Log($"[Determination] {attacker.name} gained +1 AP (trigger {_triggersThisTurn}/{maxTriggersPerTurn}).");
#endif
            }
        }
    }
}