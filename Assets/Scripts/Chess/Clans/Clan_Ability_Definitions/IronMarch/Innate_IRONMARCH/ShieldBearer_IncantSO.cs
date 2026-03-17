using UnityEngine;
using Card;

namespace Chess
{
    [CreateAssetMenu(menuName = "Chess/Piece Abilities/Shield Bearer Incant")]
    public sealed class ShieldBearer_IncantSO : PieceAbilitySO
    {
        [SerializeField] int fortifyGain = 2;
        [SerializeField] int maxFortify = 99;

        public override void OnSpellCardPlayed(PieceCtx ctx, Card.Card card, SpellCardPlayReport report)
        {
            if (ctx.piece == null) return;
            if (!report.resolved) return;

            FortifyStatusUtility.AddFortify(ctx.piece, fortifyGain, maxFortify);
        }
    }
}