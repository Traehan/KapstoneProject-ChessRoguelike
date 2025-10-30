// Assets/Scripts/Chess/SlidingMoves.cs
using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    /// <summary>
    /// Utility helpers for sliding pieces (Rook/Bishop/Queen).
    /// Keeps GetLegalMoves() bodies concise and consistent.
    /// </summary>
    internal static class SlidingMoves
    {
        /// <param name="buffer">Target list to fill.</param>
        /// <param name="self">The Piece generating moves.</param>
        /// <param name="dirs">Unit directions to slide along.</param>
        /// <param name="maxStride">Max squares per direction (e.g., 8 for “unlimited”).</param>
        /// <param name="passThroughFriendlies">If true, scans past friendlies but never lands on them.</param>
        public static void Fill(
            List<Vector2Int> buffer,
            Piece self,
            IReadOnlyList<Vector2Int> dirs,
            int maxStride,
            bool passThroughFriendlies)
        {
            buffer.Clear();
            if (self == null || self.Board == null) return;

            foreach (var d in dirs)
            {
                var c = self.Coord;
                int steps = 0;

                while (steps < maxStride)
                {
                    c += d;
                    steps++;

                    if (!self.Board.InBounds(c)) break;

                    if (!self.Board.IsOccupied(c))
                    {
                        buffer.Add(c);
                        continue;
                    }

                    var p = self.Board.GetPiece(c);
                    if (p.Team == self.Team)
                    {
                        if (passThroughFriendlies) continue; // scan beyond but cannot land
                        break;
                    }
                    else
                    {
                        buffer.Add(c); // capture one, then stop
                        break;
                    }
                }
            }
        }
    }
}