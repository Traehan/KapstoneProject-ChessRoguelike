using UnityEngine;
using Card;

namespace Chess
{
    [CreateAssetMenu(menuName = "Chess/Piece Abilities/War Chief Rally")]
    public sealed class WarChief_RallySO : PieceAbilitySO
    {
        [SerializeField] int fortifyGain = 5;
        [SerializeField] int maxFortify = 99;
        [SerializeField] int retaliateGain = 1;

        public override void OnUnitCardPlayed(PieceCtx ctx, Card.Card card, UnitCardPlayReport report)
        {
            if (ctx.piece == null) return;
            if (!report.resolved) return;
            if (report.spawnedPiece == null) return;

            // Optional safety: only trigger for allied summons
            if (report.ownerTeam != ctx.piece.Team) return;
            //if the piece is himself does not activate
            if (report.spawnedPiece == ctx.piece) return;

            FortifyStatusUtility.AddFortify(report.spawnedPiece, fortifyGain, maxFortify);
            RetaliateStatusUtility.AddRetaliate(report.spawnedPiece, retaliateGain);
        }
    }
}