using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    public class Knight : Piece
    {
        // Single shared table (no per-call reallocation)
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

            var def = Definition;
            bool forwardOnly = (def != null) && def.forwardOnly;
            int fwdSign = (Team == Team.White) ? +1 : -1;

            foreach (var j in Jumps)
            {
                if (forwardOnly && (int)Mathf.Sign(j.y) != fwdSign)
                    continue;

                var c = Coord + j;
                // Knights ignore blockers; only destination matters:
                // Use base helpers to keep consistency
                if (PushIfEmpty(c, buffer)) continue;
                PushIfEnemy(c, buffer);
            }
        }
    }
}