using System.Collections.Generic;
using UnityEngine;
using Card;

namespace Chess
{
    [CreateAssetMenu(
        menuName = "Chess/Abilities/Piece/Blood Court/Jester Trickery",
        fileName = "BloodCourt_JesterTrickery")]
    public sealed class BloodCourt_JesterTrickery : PieceAbilitySO
    {
        [Header("Bleed")]
        [Min(1)] public int bleedAmount = 2;

        [Header("Hint Color")]
        public Color hintColor = new Color(0.8f, 0.1f, 0.1f, 0.55f);

        public override void OnSpellCardPlayed(PieceCtx ctx, Card.Card card, SpellCardPlayReport report)
        {
            if (ctx.piece == null || ctx.board == null)
                return;

            // Only trigger for your own team's spells.
            if (report.ownerTeam != ctx.piece.Team)
                return;

            var tiles = GetFrontRectangle(ctx.piece);
            for (int i = 0; i < tiles.Count; i++)
            {
                var coord = tiles[i];
                if (!ctx.board.TryGetPiece(coord, out var target) || target == null)
                    continue;

                if (target.Team == ctx.piece.Team)
                    continue;

                var sc = target.GetComponent<StatusController>();
                if (sc == null)
                    continue;

                sc.AddStacks(StatusId.Bleed, bleedAmount);
            }
        }

        public override bool TryGetHintTiles(PieceCtx ctx, List<Vector2Int> outTiles, out Color color)
        {
            color = hintColor;
            if (ctx.piece == null || ctx.board == null)
                return false;

            var rect = GetFrontRectangle(ctx.piece);
            for (int i = 0; i < rect.Count; i++)
            {
                if (ctx.board.InBounds(rect[i]))
                    outTiles.Add(rect[i]);
            }

            return outTiles.Count > 0;
        }

        List<Vector2Int> GetFrontRectangle(Piece piece)
        {
            var results = new List<Vector2Int>(6);
            Vector2Int origin = piece.Coord;

            // White/front = +Y
            // Black/front = -Y
            int forward = piece.Team == Team.White ? 1 : -1;

            results.Add(new Vector2Int(origin.x - 1, origin.y + 1 * forward));
            results.Add(new Vector2Int(origin.x,     origin.y + 1 * forward));
            results.Add(new Vector2Int(origin.x + 1, origin.y + 1 * forward));

            results.Add(new Vector2Int(origin.x - 1, origin.y + 2 * forward));
            results.Add(new Vector2Int(origin.x,     origin.y + 2 * forward));
            results.Add(new Vector2Int(origin.x + 1, origin.y + 2 * forward));

            return results;
        }
    }
}