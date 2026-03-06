using UnityEngine;
using Chess;

namespace Card
{
    [System.Serializable]
    public class Card
    {
        public PieceDefinition Definition;
        public string Title;
        public Sprite Art;
        public int APCost;

        public Card(PieceDefinition def, int apCost = 1, Sprite artOverride = null, string titleOverride = null)
        {
            Definition = def;
            APCost = apCost;

            Title = !string.IsNullOrEmpty(titleOverride)
                ? titleOverride
                : (def != null ? def.displayName : "Unknown");

            Art = artOverride != null
                ? artOverride
                : (def != null ? def.icon : null); // key: uses PieceDefinition.icon
        }
    }
}