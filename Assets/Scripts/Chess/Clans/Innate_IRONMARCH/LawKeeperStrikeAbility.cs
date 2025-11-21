// Assets/Scripts/Chess/Abilities/LawkeeperStrikeAbility.cs
using UnityEngine;

namespace Chess
{
    [CreateAssetMenu(
        menuName = "Chess/Piece Abilities/Lawkeeper Strike",
        fileName = "PA_LawkeeperStrike")]
    public sealed class LawkeeperStrikeAbility : PieceAbilitySO
    {
        [Header("Lawkeeper Strike Settings")]
        [Tooltip("How many times this buff can apply.")]
        public int maxStacks = 3;
        [Tooltip("Fortify stacks required to trigger (usually 3).")]
        public int requiredFortify = 3;
        [Tooltip("Attack gained per trigger.")]
        public int attackPerStack = 1;

        [SerializeField, Tooltip("Runtime counter; do not touch during play.")]
        private int stacksGained = 0;

        public override void OnAttackResolved(PieceCtx ctx, AttackCtx atk)
        {
            var piece = ctx.piece;
            if (piece == null) return;

            // Only when THIS piece attacks
            if (atk.attacker != piece) return;

            if (stacksGained >= maxStacks) return;
            if (piece.fortifyStacks < requiredFortify) return;

            // Permanently buff this piece's attack for the encounter
            piece.attack += attackPerStack;

            // Also sync PieceRuntime.Attack if present (for other systems that read it)
            var runtime = piece.GetComponent<PieceRuntime>();
            if (runtime != null)
            {
                runtime.Attack += attackPerStack;
            }

            stacksGained++;
#if UNITY_EDITOR
            Debug.Log($"[LawkeeperStrike] {piece.name} gained +{attackPerStack} ATK ({stacksGained}/{maxStacks} stacks).");
#endif
        }

        public override void OnUndo(PieceCtx ctx)
        {
            // You could get fancy and roll back the last stack here if the kill
            // was undone, but for now we keep it simple and leave stacks as-is.
        }
    }
}