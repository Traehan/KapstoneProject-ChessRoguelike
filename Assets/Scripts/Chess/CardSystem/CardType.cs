namespace Card
{
    public enum CardType
    {
        Unit = 0,
        Spell = 1
    }

    public enum CardTargetingMode
    {
        None = 0,          // no target needed
        Tile = 1,          // target a board tile
        AlliedPiece = 2,   // target ally
        EnemyPiece = 3,    // target enemy
        AnyPiece = 4       // target either
    }
}