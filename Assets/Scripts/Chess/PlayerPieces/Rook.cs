using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    public class Rook : Piece
    {
        // Orthogonals: E, W, N, S
        static readonly Vector2Int[] DIRS = {
            new Vector2Int(+1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, +1),
            new Vector2Int(0, -1),
        };

        public override void GetLegalMoves(List<Vector2Int> buffer)
        {
            buffer.Clear();

            foreach (var d in DIRS)
            {
                var c = Coord + d;
                while (Board.InBounds(c))
                {
                    if (!Board.IsOccupied(c))
                    {
                        buffer.Add(c);
                        c += d;
                        continue;
                    }

                    var p = Board.GetPiece(c);
                    if (p != null && p.Team != Team) buffer.Add(c);
                    break;
                }
            }
        }
    }
}

