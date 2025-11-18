// Assets/Scripts/Enemies/EnemyRookForwardBehavior.cs
using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    [RequireComponent(typeof(Piece))]
    public class EnemyRookForwardBehavior : MonoBehaviour, IEnemyBehavior
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

            // Split legal moves into forward (0, +/−y) and lateral (+/−x, 0)
            var forward = new List<Vector2Int>();
            var lateral = new List<Vector2Int>();
            foreach (var c in legal)
            {
                int dx = c.x - self.Coord.x;
                int dy = c.y - self.Coord.y;
                if (dx == 0 && Mathf.Sign(dy) == fwdSign) forward.Add(c);
                else if (dy == 0 && dx != 0) lateral.Add(c);
            }

            // Prefer captures among forward; else farthest forward run
            if (TryChoose(forward, board, self, preferClosestCapture:true, out dest))
                return true;

            // If no forward option, fall back to lateral (original fallback)
            if (TryChoose(lateral, board, self, preferClosestCapture:true, out dest))
                return true;

            return false;
        }

        static bool TryChoose(List<Vector2Int> candidates, ChessBoard board, Piece self,
                              bool preferClosestCapture, out Vector2Int choice)
        {
            choice = default;
            if (candidates == null || candidates.Count == 0) return false;

            Vector2Int? bestCap = null;  int bestCapDist = int.MaxValue;
            Vector2Int? bestRun = null;  int bestRunDist = -1;

            foreach (var c in candidates)
            {
                int dist = Mathf.Abs(c.x - self.Coord.x) + Mathf.Abs(c.y - self.Coord.y);
                if (board.TryGetPiece(c, out var occ))
                {
                    if (occ.Team != self.Team && preferClosestCapture)
                    {
                        if (dist < bestCapDist) { bestCapDist = dist; bestCap = c; }
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
