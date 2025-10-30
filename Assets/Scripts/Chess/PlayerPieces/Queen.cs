using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    /// <summary>
    /// Player Queen: slides like Rook + Bishop. 
    /// Honors PieceDefinition.maxStride and passThroughFriendlies.
    /// </summary>
    public class Queen : Piece
    {
        // 8 sliding directions: rook (4) + bishop (4)
        static readonly Vector2Int[] Dirs = new[]
        {
            new Vector2Int( 1,  0), // E
            new Vector2Int(-1,  0), // W
            new Vector2Int( 0,  1), // N
            new Vector2Int( 0, -1), // S
            new Vector2Int( 1,  1), // NE
            new Vector2Int(-1,  1), // NW
            new Vector2Int( 1, -1), // SE
            new Vector2Int(-1, -1), // SW
        };

        public override void GetLegalMoves(List<Vector2Int> buffer)
        {
            buffer.Clear();
            if (Board == null) return;

            // Pull movement profile from the definition (already wired by base Piece)
            int stride = Mathf.Max(1, Definition != null ? Definition.maxStride : 8);
            bool passFriends = (Definition != null) && Definition.passThroughFriendlies;

            foreach (var d in Dirs)
            {
                for (int step = 1; step <= stride; step++)
                {
                    var c = Coord + d * step;
                    if (!Board.InBounds(c)) break;

                    var there = Board.GetPiece(c);
                    if (there == null)
                    {
                        // empty tile -> legal, keep sliding
                        buffer.Add(c);
                        continue;
                    }

                    // occupied
                    if (there.Team == Team)
                    {
                        // ally blocks unless explicitly allowed to pass
                        if (!passFriends) break;

                        // if passing friendlies is enabled, you may continue
                        // but you cannot land on the ally tile
                        continue;
                    }
                    else
                    {
                        // enemy -> can capture, but must stop afterwards
                        buffer.Add(c);
                        break;
                    }
                }
            }
        }
    }
}
