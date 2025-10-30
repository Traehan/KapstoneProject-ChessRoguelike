using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    public class EnemyBishop : Piece
    {
        // Uses Definition.forwardOnly, Definition.passThroughFriendlies, Definition.maxStride

        private static readonly Vector2Int[] Dirs =
        {
            new Vector2Int(+1, +1), new Vector2Int(-1, +1),
            new Vector2Int(+1, -1), new Vector2Int(-1, -1),
        };

        public override void GetLegalMoves(List<Vector2Int> buffer)
        {
            buffer.Clear();
            if (Board == null) return;

            // Pull movement profile from PieceDefinition (fallbacks preserve old behavior)
            var def          = Definition;
            bool forwardOnly = def ? def.forwardOnly           : true;
            bool canPass     = def ? def.passThroughFriendlies : true;
            int  stride      = def ? Mathf.Max(1, def.maxStride) : 3;

            // Direction filter if forward-only
            IEnumerable<Vector2Int> dirs = Dirs;
            if (forwardOnly)
            {
                int fwd = (Team == Team.White) ? +1 : -1;
                var list = new List<Vector2Int>(4);
                foreach (var d in Dirs)
                    if ((int)Mathf.Sign(d.y) == fwd) list.Add(d);
                dirs = list;
            }

            // Slide in allowed directions
            foreach (var d in dirs)
            {
                for (int step = 1; step <= stride; step++)
                {
                    var c = Coord + d * step;
                    if (!Board.InBounds(c)) break;

                    var occ = Board.GetPiece(c);

                    if (occ == null)
                    {
                        buffer.Add(c);
                        continue;
                    }

                    // Friendly in the way
                    if (occ.Team == Team)
                    {
                        if (canPass) continue; // skip landing, keep scanning
                        break;
                    }

                    // Enemy: capture and stop this ray
                    buffer.Add(c);
                    break;
                }
            }
        }
    }
}
