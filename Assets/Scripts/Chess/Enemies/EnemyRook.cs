using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    public class EnemyRook : Piece
    {
        // Uses Definition.forwardOnly, Definition.passThroughFriendlies, Definition.maxStride

        private static readonly Vector2Int[] AllDirs =
        {
            new Vector2Int(+1, 0), new Vector2Int(-1, 0),
            new Vector2Int(0, +1), new Vector2Int(0, -1),
        };

        private static readonly Vector2Int[] LateralDirs =
        {
            new Vector2Int(+1, 0), new Vector2Int(-1, 0),
        };

        public override void GetLegalMoves(List<Vector2Int> buffer)
        {
            buffer.Clear();
            if (Board == null) return;

            var def          = Definition;
            bool forwardOnly = def ? def.forwardOnly           : true;
            bool canPass     = def ? def.passThroughFriendlies : true;
            int  stride      = def ? Mathf.Max(1, def.maxStride) : 3;

            if (!forwardOnly)
            {
                // Standard rook slides in all 4 orthogonal directions
                Slide(AllDirs, stride, canPass, buffer);
                return;
            }

            // Forward-first behavior: try forward; if blocked, allow lateral only (never backwards)
            int fwdSign = (Team == Team.White) ? +1 : -1;
            var forwardDir = new Vector2Int(0, fwdSign);

            var tmp = new List<Vector2Int>(8);
            Slide(new[] { forwardDir }, stride, canPass, tmp);

            if (tmp.Count > 0)
            {
                buffer.AddRange(tmp);
                return;
            }

            // No forward squares -> allow left/right only
            Slide(LateralDirs, stride, canPass, buffer);
        }

        private void Slide(IEnumerable<Vector2Int> dirs, int stride, bool canPass, List<Vector2Int> outBuffer)
        {
            foreach (var d in dirs)
            {
                for (int step = 1; step <= stride; step++)
                {
                    var c = Coord + d * step;
                    if (!Board.InBounds(c)) break;

                    var occ = Board.GetPiece(c);
                    if (occ == null)
                    {
                        outBuffer.Add(c);
                        continue;
                    }

                    if (occ.Team == Team)
                    {
                        if (canPass) continue; // skip landing; keep scanning
                        break;
                    }

                    outBuffer.Add(c); // capture
                    break;
                }
            }
        }
    }
}
