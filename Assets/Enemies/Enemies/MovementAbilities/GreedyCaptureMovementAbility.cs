using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    /// <summary>
    /// Ability version of EnemyGreedyCapture: prefer the closest capture, otherwise advance as
    /// far as possible. Ported so this archetype can be authored as an EnemyPieceDefinition
    /// passive instead of a separate MonoBehaviour/tier-switch component.
    /// </summary>
    [CreateAssetMenu(menuName = "Chess/Piece Abilities/Movement/Greedy Capture", fileName = "PA_GreedyCapture")]
    public sealed class GreedyCaptureMovementAbility : PieceAbilitySO
    {
        static readonly List<Vector2Int> buf = new();

        public override bool TryGetMovementDestination(PieceCtx ctx, out Vector2Int dest)
        {
            dest = default;
            var self = ctx.piece;
            var board = ctx.board;
            if (self == null || board == null) return false;

            self.GetLegalMoves(buf);
            if (buf.Count == 0) return false;

            Vector2Int? bestCapture = null;
            int bestCaptureDist = int.MaxValue;

            Vector2Int? bestRun = null;
            int bestRunDist = -1;

            foreach (var c in buf)
            {
                int dist = Mathf.Abs(c.x - self.Coord.x) + Mathf.Abs(c.y - self.Coord.y);
                if (board.TryGetPiece(c, out var occ))
                {
                    if (occ.Team != self.Team)
                    {
                        if (dist < bestCaptureDist) { bestCaptureDist = dist; bestCapture = c; }
                    }
                }
                else
                {
                    if (dist > bestRunDist) { bestRunDist = dist; bestRun = c; }
                }
            }

            if (bestCapture.HasValue)
            {
                dest = bestCapture.Value;
                if (self is EnemyKnight ek) ek.SetLockedIntent(dest);
                return true;
            }

            if (bestRun.HasValue)
            {
                dest = bestRun.Value;
                if (self is EnemyKnight ek) ek.SetLockedIntent(dest);
                return true;
            }

            return false;
        }
    }
}
