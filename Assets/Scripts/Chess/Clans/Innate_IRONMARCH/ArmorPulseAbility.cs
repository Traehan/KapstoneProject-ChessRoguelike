// Assets/Scripts/Chess/Abilities/BulwarkPulseAbility.cs
using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    [CreateAssetMenu(
        menuName = "Chess/Piece Abilities/Armor Pulse",
        fileName = "PA_ArmorPulse")]
    public sealed class ArmorPulseAbility : PieceAbilitySO
    {
        [Header("Armor Pulse Settings")]
        [Tooltip("Fortify granted to each nearby ally at end of this unit's turn.")]
        public int fortifyPerAlly = 1;

        [Tooltip("Maximum Fortify stacks allowed on an ally (soft cap).")]
        public int maxFortify = 3;

        [Tooltip("Highlight color for the aura radius (optional).")]
        public Color auraColor = new Color(0.4f, 0.8f, 1f, 0.25f);

        public override void OnEndPlayerTurn(PieceCtx ctx)
        {
            var tm    = ctx.tm;
            var board = ctx.board;
            var piece = ctx.piece;

            if (tm == null || board == null || piece == null) return;
            if (!tm.IsPlayerTurn) return;                     // only when player ends their turn
            if (piece.Team != tm.PlayerTeam) return;

            Vector2Int origin = piece.Coord;

            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                Vector2Int pos = origin + new Vector2Int(dx, dy);
                if (!board.InBounds(pos)) continue;
                if (!board.TryGetPiece(pos, out var ally)) continue;
                if (ally.Team != piece.Team) continue;

                ally.fortifyStacks = Mathf.Min(maxFortify, ally.fortifyStacks + fortifyPerAlly);
            }
        }

        public override bool TryGetHintTiles(PieceCtx ctx, List<Vector2Int> outTiles, out Color color)
        {
            color = auraColor;
            var board = ctx.board;
            var piece = ctx.piece;
            if (board == null || piece == null) return false;

            Vector2Int origin = piece.Coord;

            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                Vector2Int pos = origin + new Vector2Int(dx, dy);
                if (!board.InBounds(pos)) continue;
                outTiles.Add(pos);
            }

            return outTiles.Count > 0;
        }
    }
}
