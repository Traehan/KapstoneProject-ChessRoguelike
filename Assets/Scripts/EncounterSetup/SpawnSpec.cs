using System;
using UnityEngine;

namespace Chess
{
    [Serializable]
    public struct SpawnSpec
    {
        public PieceDefinition piece;       // which enemy “type” to spawn (SO)
        public Vector2Int coord;            // absolute board coords (0..cols-1, 0..rows-1)

        [Tooltip("If true, coord.y is interpreted from the TOP of the board (rows-1 - y).")]
        public bool fromTop;                // handy for enemy-side placement

        public Vector2Int Resolve(ChessBoard board)
        {
            if (!fromTop) return coord;
            return new Vector2Int(coord.x, (board.rows - 1) - coord.y);
        }
    }
}

