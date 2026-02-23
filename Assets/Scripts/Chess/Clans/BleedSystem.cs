using Chess;

public static class BleedSystem
{
    public static void TickAll(ChessBoard board)
    {
        if (board == null) return;

        foreach (var p in board.GetAllPieces())
        {
            if (p == null) continue;
            var bleed = p.GetComponent<BleedStatus>();
            if (bleed != null && bleed.Stacks > 0)
                bleed.Tick(board);
        }
    }
}