using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    public class Pawn : Piece
    {
        public bool hasMoved = false;

        public override void GetLegalMoves(List<Vector2Int> buffer)
        {
            buffer.Clear();

            int dir = (Team == Team.White) ? +1 : -1;
            var def = Definition;
            int firstMoveStride = (def != null) ? Mathf.Max(1, def.maxStride) : 1;
            int stride = hasMoved ? 1 : firstMoveStride;

            // Forward squares (no captures forward)
            for (int step = 1; step <= stride; step++)
            {
                var c = new Vector2Int(Coord.x, Coord.y + dir * step);
                if (!Board.InBounds(c)) break;

                // must be empty to continue stepping forward
                if (Board.IsOccupied(c)) break;

                buffer.Add(c);
            }

            // Diagonal captures (one step)
            var diagL = new Vector2Int(Coord.x - 1, Coord.y + dir);
            var diagR = new Vector2Int(Coord.x + 1, Coord.y + dir);

            if (Board.InBounds(diagL))
            {
                var p = Board.GetPiece(diagL);
                if (p != null && p.Team != Team) buffer.Add(diagL);
            }
            if (Board.InBounds(diagR))
            {
                var p = Board.GetPiece(diagR);
                if (p != null && p.Team != Team) buffer.Add(diagR);
            }
        }


        public void MarkMoved() => hasMoved = true;
        
        protected override void OnAfterBoardMove()
        {
            // If you already have a method, call it:
            // MarkMoved();

            // Or directly:
            hasMoved = true;
        }

    }
}

