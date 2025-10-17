using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    public class EnemyRook : Piece
    {
        [Header("Enemy Movement")]
        [SerializeField, Min(1)] int maxStride = 3;
        [SerializeField] bool passThroughFriendlies = true;
        [SerializeField] bool forwardOnly = true;  // “never move backwards”; rook can go side-to-side

        static readonly Vector2Int[] DIRS = {
            new Vector2Int(+1, 0), new Vector2Int(-1, 0),
            new Vector2Int(0, +1), new Vector2Int(0, -1),
        };

        public override void GetLegalMoves(List<Vector2Int> buffer)
{
    buffer.Clear();
    
    
    var def = Definition; // read-only property from Piece
    int  stride         = (def != null) ? def.maxStride             : maxStride;
    bool canPassThrough = (def != null) ? def.passThroughFriendlies : passThroughFriendlies;
    bool onlyForward    = (def != null) ? def.forwardOnly           : forwardOnly;

    int fwdSign = (Team == Team.White) ? +1 : -1;
    Vector2Int forwardDir = new Vector2Int(0, fwdSign);

    // 1) Try to build FORWARD moves first (up to stride, no backwards)
    var forwardMoves = new List<Vector2Int>();
    {
        var c = Coord;
        int steps = 0;
        while (steps < stride)
        {
            c += forwardDir;
            steps++;
            if (!Board.InBounds(c)) break;

            var occ = Board.GetPiece(c);
            if (occ == null)
            {
                forwardMoves.Add(c);
                continue; // keep sliding forward
            }

            if (occ.Team == Team)
            {
                // friendly: can pass through if allowed, but cannot land
                if (canPassThrough) continue;
                break; // blocked
            }
            else
            {
                // enemy: can capture, then stop
                forwardMoves.Add(c);
                break;
            }
        }
    }

    // If any forward move exists, ONLY allow forward this turn.
    if (forwardMoves.Count > 0)
    {
        buffer.AddRange(forwardMoves);
        return;
    }

    // 2) No forward moves? Then allow SIDE-TO-SIDE (left/right), never backward.
    Vector2Int[] lateralDirs = { new Vector2Int(+1, 0), new Vector2Int(-1, 0) };

    foreach (var d in lateralDirs)
    {
        var c = Coord;
        int steps = 0;
        while (steps < stride)
        {
            c += d;
            steps++;
            if (!Board.InBounds(c)) break;

            var occ = Board.GetPiece(c);
            if (occ == null)
            {
                buffer.Add(c);
                continue; // keep sliding
            }

            if (occ.Team == Team)
            {
                if (canPassThrough) continue; // skip landing; keep scanning
                break;
            }
            else
            {
                buffer.Add(c); // capture then stop in this dir
                break;
            }
        }
    }
}
    }
}