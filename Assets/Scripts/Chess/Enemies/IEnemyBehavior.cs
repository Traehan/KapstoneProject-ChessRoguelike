using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    public interface IEnemyBehavior
    {
        /// <summary>Return desired tile for this enemy piece on its turn.</summary>
        bool TryGetDesiredDestination(ChessBoard board, out Vector2Int dest);
    }

    /// <summary>
    /// Optional extension for enemies that want to provide a richer set of
    /// "intent tiles" (for telegraphs / warnings).
    ///
    /// - If present, TurnManager will use this instead of the single dest tile
    ///   from IEnemyBehavior when painting enemy intent highlights.
    /// </summary>
    public interface IEnemyIntentProvider
    {
        /// <summary>
        /// Fill 'buffer' with all tiles this enemy threatens next enemy turn.
        /// Do NOT clear the buffer in the caller; the provider is responsible
        /// for clearing/overwriting as needed.
        /// </summary>
        void GetIntentTiles(ChessBoard board, List<Vector2Int> buffer);
    }
}