using UnityEngine;
using Card;

namespace Chess
{
    [CreateAssetMenu(
        menuName = "Chess/Abilities/Piece/Blood Court/Funeral Harvest",
        fileName = "BloodCourt_FuneralHarvest")]
    public sealed class BloodCourt_FuneralHarvest : PieceAbilitySO
    {
        [Header("Draw")]
        [Min(1)] public int cardsPerDeath = 1;

        [Header("Range")]
        [Tooltip("Chebyshev distance. 1 = surrounding 8 tiles.")]
        [Min(1)] public int radius = 1;

        bool IsWithinRadius(Vector2Int a, Vector2Int b, int r)
        {
            return Mathf.Abs(a.x - b.x) <= r && Mathf.Abs(a.y - b.y) <= r;
        }

        public override void OnPieceCaptured(PieceCtx ctx, Piece victim, Piece by, Vector2Int at)
        {
            if (ctx.piece == null || ctx.tm == null || victim == null)
                return;

            // Only care about allied deaths, not self, not enemy deaths
            if (victim.Team != ctx.piece.Team)
                return;

            if (victim == ctx.piece)
                return;

            // Must be surrounding / nearby
            if (!IsWithinRadius(ctx.piece.Coord, at, radius))
                return;

            // If death happens during Spell Phase, draw immediately
            if (ctx.tm.Phase == TurnPhase.SpellPhase)
            {
                var deck = Object.FindObjectOfType<DeckManager>();
                if (deck != null)
                    deck.Draw(cardsPerDeath);

                var hand = Object.FindObjectOfType<HandPanel>();
                if (hand != null)
                    hand.RebuildHand();

                return;
            }

            // If death happens during battle/enemy flow, queue it for next Spell Phase
            ctx.tm.QueueNextSpellPhaseDraw(cardsPerDeath);
        }
    }
}