// Assets/Scripts/Chess/Abilities/PieceSpawnUtil.cs
using UnityEngine;

namespace Chess
{
    /// <summary>
    /// Small helper for places that spawn pieces. You can use this, or just call PieceRuntime.Init directly.
    /// PieceRuntime.Init already consumes queued upgrades.
    /// </summary>
    public static class PieceSpawnUtil
    {
        public static void InitRuntime(Piece piece, ChessBoard board, TurnManager tm)
        {
            if (piece == null) return;

            var runtime = piece.GetComponent<PieceRuntime>();
            if (runtime == null) runtime = piece.gameObject.AddComponent<PieceRuntime>();
            runtime.Init(piece, board, tm);
        }
    }
}