using Chess;

namespace Card
{
    [System.Serializable]
    public struct TransferFortifyTarget
    {
        public Piece source;
        public Piece destination;

        public TransferFortifyTarget(Piece source, Piece destination)
        {
            this.source = source;
            this.destination = destination;
        }
    }
}