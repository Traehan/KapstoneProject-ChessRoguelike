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
        
        // EnemyKnight.cs (inside class)
        public bool HasLockedIntent { get; private set; }
        public Vector2Int LockedIntent { get; private set; }

        public void SetLockedIntent(Vector2Int dest)
        {
            LockedIntent = dest;
            HasLockedIntent = true;
        }

        public void ClearLockedIntent()
        {
            HasLockedIntent = false;
        }

        public override void GetLegalMoves(List<Vector2Int> buffer)
        {
            buffer.Clear();
            int fwdSign = (Team == Team.White) ? +1 : -1;

            // When forwardOnly: collect candidates into buckets, then pick ONE forward at random.
            // Otherwise: return all legal jumps.
            if (forwardOnly)
            {
                List<Vector2Int> forwardOpts = new List<Vector2Int>(4);
                List<Vector2Int> otherOpts   = new List<Vector2Int>(4);

                foreach (var j in JUMPS)
                {
                    var c = Coord + j;
                    if (!Board.InBounds(c)) continue;

                    var occ = Board.GetPiece(c);
                    bool landable = (occ == null || occ.Team != Team); // empty or capture
                    if (!landable) continue;

                    bool isForward = Mathf.Sign(j.y) == Mathf.Sign(fwdSign);
                    if (isForward) forwardOpts.Add(c);
                    else           otherOpts.Add(c);
                }

                if (forwardOpts.Count > 0)
                {
                    // Pick exactly one forward move so EnemySimpleChooser can't bias a direction.
                    int idx = Random.Range(0, forwardOpts.Count);
                    buffer.Add(forwardOpts[idx]);
                }
                else
                {
                    // No forward options—allow any other legal knight jump to avoid freezing.
                    buffer.AddRange(otherOpts);
                }
                return;
            }

            // Non-forward-only: standard knight moves (all legal jumps).
            foreach (var j in JUMPS)
            {
                var c = Coord + j;
                if (!Board.InBounds(c)) continue;

                var occ = Board.GetPiece(c);
                if (occ == null || occ.Team != Team)
                    buffer.Add(c);
            }
        }
    }
}