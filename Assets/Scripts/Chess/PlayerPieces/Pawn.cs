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
            int dir = (int)Team; // White=+1, Black=-1

            // Forward 1
            var f1 = new Vector2Int(Coord.x, Coord.y + dir);
            if (PushIfEmpty(f1, buffer))
            {
                // Forward 2 if on starting rank and both empty
                if (!hasMoved)
                {
                    var f2 = new Vector2Int(Coord.x, Coord.y + 2 * dir);
                    if (Board.InBounds(f2) && !Board.IsOccupied(f2))
                        buffer.Add(f2);
                }
            }

            // Diagonal captures
            var dl = new Vector2Int(Coord.x - 1, Coord.y + dir);
            var dr = new Vector2Int(Coord.x + 1, Coord.y + dir);
            PushIfEnemy(dl, buffer);
            PushIfEnemy(dr, buffer);
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

