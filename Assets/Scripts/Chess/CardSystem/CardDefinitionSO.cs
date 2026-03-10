using UnityEngine;

namespace Card
{
    public abstract class CardDefinitionSO : ScriptableObject
    {
        [Header("Core")]
        public string cardId;
        public string displayName;
        [TextArea] public string rulesText;
        public Sprite art;
        public int manaCost = 1;

        [Header("Card Identity")]
        public CardType cardType = CardType.Unit;
        public CardTargetingMode targetingMode = CardTargetingMode.Tile;

        public virtual string GetDisplayName()
        {
            return string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        }

        public virtual Sprite GetArt()
        {
            return art;
        }
    }
}