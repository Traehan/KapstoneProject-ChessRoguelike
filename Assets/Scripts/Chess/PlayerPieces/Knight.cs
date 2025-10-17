using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    public class Knight : Piece
    {
        // L-jumps
        static readonly Vector2Int[] JUMPS = {
            new Vector2Int(+1, +2), new Vector2Int(+2, +1),
            new Vector2Int(+2, -1), new Vector2Int(+1, -2),
            new Vector2Int(-1, -2), new Vector2Int(-2, -1),
            new Vector2Int(-2, +1), new Vector2Int(-1, +2),
        };

        public override void GetLegalMoves(List<Vector2Int> buffer)
        {
            buffer.Clear();

            // L-jumps
            Vector2Int[] JUMPS = {
                new Vector2Int(+1, +2), new Vector2Int(+2, +1),
                new Vector2Int(+2, -1), new Vector2Int(+1, -2),
                new Vector2Int(-1, -2), new Vector2Int(-2, -1),
                new Vector2Int(-2, +1), new Vector2Int(-1, +2),
            };

            var def = Definition;
            bool onlyForward = (def != null) ? def.forwardOnly : false;

            int fwdSign = (Team == Team.White) ? +1 : -1;

            foreach (var j in JUMPS)
            {
                // If forwardOnly, keep jumps whose Y component goes "forward"
                if (onlyForward && (int)Mathf.Sign(j.y) != fwdSign) continue;

                var c = Coord + j;
                if (!Board.InBounds(c)) continue;

                // Knight can jump over pieces; only the destination matters.
                if (!Board.IsOccupied(c)) { buffer.Add(c); continue; }

                var p = Board.GetPiece(c);
                if (p != null && p.Team != Team) buffer.Add(c);
            }
        }
    }
}