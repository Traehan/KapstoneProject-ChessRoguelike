using UnityEngine;
using Chess;

namespace Card
{
    [System.Serializable]
    public struct FortifyShiftTarget
    {
        public Piece piece;
        public Vector2Int destination;

        public FortifyShiftTarget(Piece piece, Vector2Int destination)
        {
            this.piece = piece;
            this.destination = destination;
        }
    }
}