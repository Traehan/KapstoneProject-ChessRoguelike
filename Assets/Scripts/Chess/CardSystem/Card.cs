using Chess;

namespace Card
{
    [System.Serializable]
    public class Card
    {
        public PieceDefinition Definition;

        public Card(PieceDefinition def)
        {
            Definition = def;
        }
    }
}