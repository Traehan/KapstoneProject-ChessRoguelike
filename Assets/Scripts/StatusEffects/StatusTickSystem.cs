using System.Collections.Generic;
using System.Linq;
using Chess;
using UnityEngine;

public static class StatusTickSystem
{
    // Ticks ONLY the things you want at end of player turn.
    public static void TickEndOfPlayerTurn(ChessBoard board)
    {
        if (board == null) return;

        // IMPORTANT: snapshot the pieces first so removing doesn't modify the collection we're iterating.
        var pieces = board.GetAllPieces().Where(p => p != null).ToList();

        // collect deaths and remove AFTER we finish ticking
        List<Piece> toRemove = null;

        for (int i = 0; i < pieces.Count; i++)
        {
            var p = pieces[i];
            if (p == null) continue;

            var sc = p.GetComponent<StatusController>();
            if (sc == null) continue;

            int bleed = sc.GetStacks(StatusId.Bleed);
            if (bleed <= 0) continue;

            p.currentHP -= bleed;
            Debug.Log("Enemy took damage from bleed effect");

            GameEvents.OnPieceDamaged?.Invoke(p, bleed, null);
            GameEvents.OnPieceStatsChanged?.Invoke(p);

            if (p.currentHP <= 0)
            {
                toRemove ??= new List<Piece>();
                toRemove.Add(p);
            }
        }

        if (toRemove == null) return;

        // Remove after iteration to avoid "collection modified" exception
        for (int i = 0; i < toRemove.Count; i++)
        {
            var p = toRemove[i];
            if (p == null) continue;
            board.RemovePiece(p);
        }
    }
}