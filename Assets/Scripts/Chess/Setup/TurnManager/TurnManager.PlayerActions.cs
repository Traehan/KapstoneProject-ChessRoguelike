using UnityEngine;

namespace Chess
{
    public partial class TurnManager
    {
        bool ValidatePlayerMove(Piece piece, Vector2Int dest)
        {
            return Phase == TurnPhase.PlayerTurn &&
                   piece != null &&
                   board != null &&
                   CurrentAP > 0 &&
                   board.InBounds(dest);
        }
    }
}