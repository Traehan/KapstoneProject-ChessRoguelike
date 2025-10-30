using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    public class Bishop : Piece
    {
        private static readonly Vector2Int[] Dirs =
        {
            new Vector2Int(+1, +1),
            new Vector2Int(-1, +1),
            new Vector2Int(+1, -1),
            new Vector2Int(-1, -1),
        };

        public override void GetLegalMoves(List<Vector2Int> buffer)
        {
            var def = Definition;
            int  stride         = (def != null) ? Mathf.Max(1, def.maxStride) : 8;
            bool passThrough    = (def != null) && def.passThroughFriendlies;

            SlidingMoves.Fill(buffer, this, Dirs, stride, passThrough);
        }
    }
}