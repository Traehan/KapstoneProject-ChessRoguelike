using Chess;

namespace Card
{
    [System.Serializable]
    public struct PhalanxRotateTarget
    {
        public Piece first;
        public Piece second;

        public PhalanxRotateTarget(Piece first, Piece second)
        {
            this.first = first;
            this.second = second;
        }
    }
}