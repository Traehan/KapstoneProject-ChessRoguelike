using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    public class EnemyKnight : Piece
    {
        [Header("Enemy Movement")]
        [SerializeField] bool forwardOnly = true;  // no backwards jumps

        static readonly Vector2Int[] JUMPS = {
            new Vector2Int(+1, +2), new Vector2Int(+2, +1),
            new Vector2Int(+2, -1), new Vector2Int(+1, -2),
            new Vector2Int(-1, -2), new Vector2Int(-2, -1),
            new Vector2Int(-2, +1), new Vector2Int(-1, +2),
        };

        public override void GetLegalMoves(List<Vector2Int> buffer)
        {
            buffer.Clear();
            int fwdSign = (Team == Team.White) ? +1 : -1;

            foreach (var j in JUMPS)
            {
                if (forwardOnly && Mathf.Sign(j.y) != Mathf.Sign(fwdSign)) continue;

                var c = Coord + j;
                if (!Board.InBounds(c)) continue;

                var occ = Board.GetPiece(c);
                if (occ == null || occ.Team != Team) buffer.Add(c); // empty or capture
            }
        }
    }
}