using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    public class Bishop : Piece
    {
        // Diagonals: NE, NW, SE, SW
        static readonly Vector2Int[] DIRS = {
            new Vector2Int(+1, +1),
            new Vector2Int(-1, +1),
            new Vector2Int(+1, -1),
            new Vector2Int(-1, -1),
        };

        public override void GetLegalMoves(List<Vector2Int> buffer)
        {
            buffer.Clear();

            var def = Definition;
            int  stride         = (def != null) ? Mathf.Max(1, def.maxStride) : 8;  // default 8 on 8x8
            bool canPassThrough = (def != null) ? def.passThroughFriendlies   : false;

            // Diagonals: NE, NW, SE, SW
            Vector2Int[] DIRS = {
                new Vector2Int(+1, +1),
                new Vector2Int(-1, +1),
                new Vector2Int(+1, -1),
                new Vector2Int(-1, -1),
            };

            foreach (var d in DIRS)
            {
                var c = Coord;
                int steps = 0;

                while (steps < stride)
                {
                    c += d;
                    steps++;

                    if (!Board.InBounds(c)) break;

                    if (!Board.IsOccupied(c))
                    {
                        buffer.Add(c);     // empty → can land; keep sliding
                        continue;
                    }

                    var p = Board.GetPiece(c);
                    if (p.Team == Team)
                    {
                        // friendly: cannot land; can optionally scan past
                        if (canPassThrough) continue;
                        break;
                    }
                    else
                    {
                        // enemy: can capture, then stop in this dir
                        buffer.Add(c);
                        break;
                    }
                }
            }
        }

    }
}