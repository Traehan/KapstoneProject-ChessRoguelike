// Assets/Scripts/Enemies/EnemyGreedyCapture.cs
using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    [RequireComponent(typeof(Piece))]
    public class EnemyGreedyCapture : MonoBehaviour, IEnemyBehavior
    {
        private Piece self;
        private static readonly List<Vector2Int> buf = new();

        void Awake() { self = GetComponent<Piece>(); }

        public bool TryGetDesiredDestination(ChessBoard board, out Vector2Int dest)
        {
            dest = default;
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
                        // prefer the closest capture so we don't skip easy kills
                        if (dist < bestCaptureDist) { bestCaptureDist = dist; bestCapture = c; }
                    }
                }
                else
                {
                    // no capture -> advance as far as possible
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