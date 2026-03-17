namespace Card
{
    public enum CardType
    {
        Unit = 0,
        Spell = 1
    }

    public enum CardTargetingMode
    {
        None = 0,                           // no target needed
        Tile = 1,                           // target a board tile
        AlliedPiece = 2,                    // single allied piece
        EnemyPiece = 3,                     // single enemy piece
        AnyPiece = 4,                       // single piece of either side

        AlliedPieceThenAdjacentEmptyTile = 10, // Fortify Shift style
        AlliedPieceThenAlliedPiece = 11,       // Pass It On style
        AlliedPieceThenAdjacentPiece = 12      // Phalanx Rotate style
    }
}