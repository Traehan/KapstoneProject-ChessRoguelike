using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    public class EnemyKnight : Piece
    {
        // Uses Definition.forwardOnly

        private static readonly Vector2Int[] Jumps =
        {
            new Vector2Int(+1, +2), new Vector2Int(+2, +1),
            new Vector2Int(+2, -1), new Vector2Int(+1, -2),
            new Vector2Int(-1, -2), new Vector2Int(-2, -1),
            new Vector2Int(-2, +1), new Vector2Int(-1, +2),
        };

        public override void GetLegalMoves(List<Vector2Int> buffer)
        {
            buffer.Clear();
            if (Board == null) return;

            var def          = Definition;
            bool forwardOnly = def ? def.forwardOnly : true; // fallback matches old behavior
            int  fwdSign     = (Team == Team.White) ? +1 : -1;

            foreach (var j in Jumps)
            {
                if (forwardOnly && ((int)Mathf.Sign(j.y) != fwdSign))
                    continue;

                var c = Coord + j;
                if (!Board.InBounds(c)) continue;

                var occ = Board.GetPiece(c);
                if (occ == null || occ.Team != Team)
                    buffer.Add(c);
            }
        }
    }
}