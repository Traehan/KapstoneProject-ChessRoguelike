using UnityEngine;

namespace Chess
{
    /// <summary>
    /// Ironclad Enforcer's "Enforce": at the start of the player's turn, if this piece currently
    /// has requiredFortify+ Fortify stacks, gain +1 Retaliate, capped at maxRetaliateStacks.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Chess/Piece Abilities/Enforce",
        fileName = "PA_Enforce")]
    public sealed class EnforceAbility : PieceAbilitySO
    {
        [Header("Enforce Settings")]
        [Tooltip("Fortify stacks required to trigger.")]
        public int requiredFortify = 3;

        [Tooltip("Retaliate stacks gained per trigger.")]
        public int retaliateGain = 1;

        [Tooltip("Maximum Retaliate stacks this ability can grant/maintain.")]
        public int maxRetaliateStacks = 3;

        public override void OnBeginPlayerTurn(PieceCtx ctx)
        {
            var piece = ctx.piece;
            if (piece == null) return;

            int fortify = FortifyStatusUtility.GetFortify(piece);
            if (fortify < requiredFortify) return;

            int current = RetaliateStatusUtility.GetRetaliate(piece);
            if (current >= maxRetaliateStacks) return;

            int next = Mathf.Min(current + retaliateGain, maxRetaliateStacks);
            RetaliateStatusUtility.SetRetaliate(piece, next);
        }
    }
}
