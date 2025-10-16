// EnemySimpleChooser.cs
using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    [RequireComponent(typeof(Piece))]
    public class EnemySimpleChooser : MonoBehaviour, IEnemyBehavior
    {
        Piece self;
        static readonly List<Vector2Int> buf = new();

        void Awake() { self = GetComponent<Piece>(); }

        public bool TryGetDesiredDestination(ChessBoard board, out Vector2Int desired)
        {
            desired = default;
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
                        // prefer the closest capture so we don’t overrun freebies
                        if (dist < bestCaptureDist) { bestCaptureDist = dist; bestCapture = c; }
                    }
                    // else friendly; not landable (legal generator already ensured this)
                }
                else
                {
                    // no capture: take farthest advance
                    if (dist > bestRunDist) { bestRunDist = dist; bestRun = c; }
                }
            }

            if (bestCapture.HasValue) { desired = bestCapture.Value; return true; }
            if (bestRun.HasValue)     { desired = bestRun.Value;     return true; }
            return false;
        }
    }
}