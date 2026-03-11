using System;
using UnityEngine;
using Chess;

namespace Card
{
    [Serializable]
    public class Card
    {
        [SerializeField] private string runtimeId;
        [SerializeField] private CardDefinitionSO definition;

        // Temporary compatibility bridge so your current troop-based run deck still works
        [SerializeField] private PieceDefinition legacyPieceDefinition;
        [SerializeField] private int legacyManaCost = 1;

        public string RuntimeId => runtimeId;
        public CardDefinitionSO Definition => definition;
        public PieceDefinition LegacyPieceDefinition => legacyPieceDefinition;

        public CardType Type
        {
            get
            {
                if (definition != null) return definition.cardType;
                return legacyPieceDefinition != null ? CardType.Unit : CardType.Spell;
            }
        }

        public int ManaCost
        {
            get
            {
                if (definition != null) return Mathf.Max(0, definition.manaCost);
                return Mathf.Max(0, legacyManaCost);
            }
        }

        public string Title
        {
            get
            {
                if (definition != null) return definition.GetDisplayName();
                if (legacyPieceDefinition != null)
                    return !string.IsNullOrWhiteSpace(legacyPieceDefinition.displayName)
                        ? legacyPieceDefinition.displayName
                        : legacyPieceDefinition.name;

                return "Unknown Card";
            }
        }

        public string RulesText
        {
            get
            {
                if (definition != null) return definition.rulesText;
                return string.Empty;
            }
        }

        public Sprite Art
        {
            get
            {
                if (definition != null) return definition.GetArt();
                return legacyPieceDefinition != null ? legacyPieceDefinition.icon : null;
            }
        }

        public CardTargetingMode TargetingMode
        {
            get
            {
                if (definition != null) return definition.targetingMode;
                return CardTargetingMode.Tile;
            }
        }

        public Card(CardDefinitionSO definition)
        {
            runtimeId = Guid.NewGuid().ToString("N");
            this.definition = definition;
        }

        public Card(PieceDefinition legacyPieceDefinition, int manaCost = 1)
        {
            runtimeId = Guid.NewGuid().ToString("N");
            this.legacyPieceDefinition = legacyPieceDefinition;
            legacyManaCost = manaCost;
        }

        public bool IsUnitCard()
        {
            return Type == CardType.Unit && GetSummonPieceDefinition() != null;
        }

        public bool IsSpellCard()
        {
            return Type == CardType.Spell;
        }

        public PieceDefinition GetSummonPieceDefinition()
        {
            if (definition is UnitCardDefinitionSO unitDef)
                return unitDef.summonPieceDefinition;

            return legacyPieceDefinition;
        }
    }
}