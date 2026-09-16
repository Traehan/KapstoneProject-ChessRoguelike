using Chess;

namespace Card
{
    /// <summary>
    /// Two-target payload for Iron Sentence: an allied piece (loses half its Fortify) and an
    /// enemy piece (takes damage equal to the Fortify lost, capped). The enemy need not be
    /// adjacent to the ally — see CardTargetingMode.AlliedPieceThenEnemyPiece.
    /// </summary>
    [System.Serializable]
    public struct IronSentenceTarget
    {
        public Piece ally;
        public Piece enemy;

        public IronSentenceTarget(Piece ally, Piece enemy)
        {
            this.ally = ally;
            this.enemy = enemy;
        }
    }
}
