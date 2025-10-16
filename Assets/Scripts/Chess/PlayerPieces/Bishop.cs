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

            foreach (var d in DIRS)
            {
                var c = Coord + d;
                while (Board.InBounds(c))
                {
                    // empty square → add and keep sliding
                    if (!Board.IsOccupied(c))
                    {
                        buffer.Add(c);
                        c += d;
                        continue;
                    }

                    // hit a piece → allow capture if enemy, then stop
                    var p = Board.GetPiece(c);
                    if (p != null && p.Team != Team) buffer.Add(c);
                    break;
                }
            }
        }
    }
}