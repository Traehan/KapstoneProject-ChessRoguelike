// Assets/Scripts/Enemies/EnemyKnightForwardBehavior.cs
using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    [RequireComponent(typeof(Piece))]
    public class EnemyKnightForwardBehavior : MonoBehaviour, IEnemyBehavior
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

            // Buckets
            Vector2Int? forwardCap = null; int forwardCapBestDist = int.MaxValue;
            Vector2Int? forwardRun = null; int forwardRunBestAdvance = int.MinValue;

            Vector2Int? anyCap = null; int anyCapBestDist = int.MaxValue;
            Vector2Int? anyRun = null; int anyRunBestDist = int.MinValue;

            foreach (var c in legal)
            {
                int dy = c.y - self.Coord.y;
                int md = Mathf.Abs(c.x - self.Coord.x) + Mathf.Abs(dy);

                bool isForward = Mathf.Sign(dy) == fwdSign;
                bool isCapture = board.TryGetPiece(c, out var occ) && occ.Team != self.Team;

                if (isForward)
                {
                    if (isCapture)
                    {
                        if (md < forwardCapBestDist) { forwardCapBestDist = md; forwardCap = c; }
                    }
                    else
                    {
                        // prefer strongest forward advance (bigger dy in forward direction)
                        int advance = fwdSign * dy;
                        if (advance > forwardRunBestAdvance) { forwardRunBestAdvance = advance; forwardRun = c; }
                    }
                }

                // global fallbacks
                if (isCapture)
                {
                    if (md < anyCapBestDist) { anyCapBestDist = md; anyCap = c; }
                }
                else
                {
                    if (md > anyRunBestDist) { anyRunBestDist = md; anyRun = c; }
                }
            }

            // Priority order
            if (forwardCap.HasValue)      dest = forwardCap.Value;
            else if (forwardRun.HasValue) dest = forwardRun.Value;
            else if (anyCap.HasValue)     dest = anyCap.Value;
            else if (anyRun.HasValue)     dest = anyRun.Value;
            else return false;

            if (self is EnemyKnight ek) ek.SetLockedIntent(dest);
            return true;
        }
    }
}
