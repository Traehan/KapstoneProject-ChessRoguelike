using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    public class EnemyBishop : Piece
    {
        [Header("Enemy Movement")]
        [SerializeField, Min(1)] int maxStride = 3;     // tweak in Inspector
        [SerializeField] bool passThroughFriendlies = true;
        [SerializeField] bool forwardOnly = true;       // no backwards

        static readonly Vector2Int[] DIRS = {
            new Vector2Int(+1, +1), new Vector2Int(-1, +1),
            new Vector2Int(+1, -1), new Vector2Int(-1, -1),
        };

        public override void GetLegalMoves(List<Vector2Int> buffer)
        {
            buffer.Clear();
            int fwdSign = (Team == Team.White) ? +1 : -1;

            foreach (var d in DIRS)
            {
                // forwardOnly: only keep diagonals that go forward relative to team
                if (forwardOnly && Mathf.Sign(d.y) != Mathf.Sign(fwdSign)) continue;

                var c = Coord;
                int steps = 0;
                while (steps < maxStride)
                {
                    c += d;
                    steps++;
                    if (!Board.InBounds(c)) break;

                    var occ = Board.GetPiece(c);
                    if (occ == null)
                    {
                        // empty: always landable
                        buffer.Add(c);
                        continue;
                    }

                    if (occ.Team == Team)
                    {
                        // friendly: can pass through if allowed, but cannot land here
                        if (passThroughFriendlies) continue;
                        else break;
                    }
                    else
                    {
                        // enemy (i.e., player's piece): capture is allowed; stop after capture
                        buffer.Add(c);
                        break;
                    }
                }
            }
        }
    }
}

