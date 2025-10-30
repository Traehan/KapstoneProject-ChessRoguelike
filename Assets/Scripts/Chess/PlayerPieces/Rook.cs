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
            if (Board == null) return;

            var def = Definition;
            int  stride         = (def != null) ? Mathf.Max(1, def.maxStride) : 8; // default unlimited on 8x8
            bool canPassThrough = (def != null) ? def.passThroughFriendlies   : false;

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
                        // Empty: can land and continue sliding
                        buffer.Add(c);
                        continue;
                    }

                    // Occupied: check team and decide whether to stop/continue
                    var p = Board.GetPiece(c);
                    if (p.Team == Team)
                    {
                        // Friendly: cannot land; optionally scan past
                        if (canPassThrough) continue;
                        break;
                    }
                    else
                    {
                        // Enemy: can capture; must stop after capturing
                        buffer.Add(c);
                        break;
                    }
                }
            }
        }
    }
}