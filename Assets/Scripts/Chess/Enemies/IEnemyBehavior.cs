using UnityEngine;

namespace Chess
{
    public interface IEnemyBehavior
    {
        /// <summary>Return desired tile for this enemy piece on its turn.</summary>
        bool TryGetDesiredDestination(ChessBoard board, out Vector2Int dest);
    }
}