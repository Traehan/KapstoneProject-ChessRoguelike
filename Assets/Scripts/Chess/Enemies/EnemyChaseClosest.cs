// Assets/Scripts/Enemies/EnemyChaseClosest.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Chess
{
    [RequireComponent(typeof(Piece))]
    public class EnemyChaseClosest : MonoBehaviour, IEnemyBehavior
    {
        private Piece self;
        private static readonly List<Vector2Int> legal = new();

        void Awake() { self = GetComponent<Piece>(); }

        public bool TryGetDesiredDestination(ChessBoard board, out Vector2Int dest)
        {
            dest = default;
            if (self == null || board == null) return false;

            // Always prefer captures (fast check)
            self.GetLegalMoves(legal);
            if (legal.Count == 0) return false;

            Vector2Int? capture = legal
                .Where(c => board.TryGetPiece(c, out var p) && p.Team != self.Team)
                .Cast<Vector2Int?>()
                .FirstOrDefault();

            if (capture.HasValue)
            {
                dest = capture.Value;
                if (self is EnemyKnight ek) ek.SetLockedIntent(dest);
                return true;
            }

            // No captures: pick the move that minimizes distance to the closest PLAYER piece
            var playerPieces = board.GetAllPieces().Where(p => p != null && p.Team != self.Team).ToList();
            if (playerPieces.Count == 0) return false;

            int BestScore(Vector2Int move)
            {
                // score = min distance from (move) to any player piece; lower is better
                int best = int.MaxValue;
                foreach (var pp in playerPieces)
                {
                    int md = Mathf.Abs(pp.Coord.x - move.x) + Mathf.Abs(pp.Coord.y - move.y);
                    if (md < best) best = md;
                }
                // Slight bias: prefer forward progress if tied
                int fwdSign = (self.Team == Team.White) ? +1 : -1;
                int forwardBias = -fwdSign * (move.y - self.Coord.y);
                return best * 10 + forwardBias;
            }

            Vector2Int bestMove = legal[0];
            int bestScore = BestScore(bestMove);

            for (int i = 1; i < legal.Count; i++)
            {
                var m = legal[i];
                int s = BestScore(m);
                if (s < bestScore) { bestScore = s; bestMove = m; }
            }

            dest = bestMove;
            if (self is EnemyKnight ek2) ek2.SetLockedIntent(dest);
            return true;
        }
    }
}
