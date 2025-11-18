// Assets/Scripts/Enemies/EnemyBishopForwardBehavior.cs
using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    [RequireComponent(typeof(Piece))]
    public class EnemyBishopForwardBehavior : MonoBehaviour, IEnemyBehavior
    {
        private Piece self;
        private static readonly List<Vector2Int> legal = new();

        void Awake() { self = GetComponent<Piece>(); }

        public bool TryGetDesiredDestination(ChessBoard board, out Vector2Int dest)
        {
            dest = default;
            if (self == null || board == null) return false;

            self.GetLegalMoves(legal);
            if (legal.Count == 0) return false;

            int fwdSign = (self.Team == Team.White) ? +1 : -1;

            // Split by forward vs. non-forward diagonals
            var forward = new List<Vector2Int>();
            var others  = new List<Vector2Int>();

            foreach (var c in legal)
            {
                int dy = c.y - self.Coord.y;
                if (Mathf.Sign(dy) == fwdSign) forward.Add(c);
                else others.Add(c);
            }

            // Prefer forward diagonals
            if (TryChoose(forward, board, self, out dest)) return true;

            // Fall back to the rest
            if (TryChoose(others, board, self, out dest)) return true;

            return false;
        }

        static bool TryChoose(List<Vector2Int> candidates, ChessBoard board, Piece self, out Vector2Int choice)
        {
            choice = default;
            if (candidates == null || candidates.Count == 0) return false;

            Vector2Int? bestCap = null; int bestCapDist = int.MaxValue;
            Vector2Int? bestRun = null; int bestRunDist = -1;

            foreach (var c in candidates)
            {
                int dist = Mathf.Abs(c.x - self.Coord.x) + Mathf.Abs(c.y - self.Coord.y);
                if (board.TryGetPiece(c, out var occ))
                {
                    if (occ.Team != self.Team && dist < bestCapDist)
                    {
                        bestCapDist = dist; bestCap = c;
                    }
                }
                else
                {
                    if (dist > bestRunDist) { bestRunDist = dist; bestRun = c; }
                }
            }

            if (bestCap.HasValue) { choice = bestCap.Value; return true; }
            if (bestRun.HasValue) { choice = bestRun.Value; return true; }
            return false;
        }
    }
}
