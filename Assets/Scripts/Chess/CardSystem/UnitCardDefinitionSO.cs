using UnityEngine;
using Chess;

namespace Card
{
    [CreateAssetMenu(menuName = "Cards/Unit Card Definition", fileName = "UnitCardDefinition")]
    public class UnitCardDefinitionSO : CardDefinitionSO
    {
        [Header("Unit Payload")]
        public PieceDefinition summonPieceDefinition;

        private void OnValidate()
        {
            cardType = CardType.Unit;
            targetingMode = CardTargetingMode.Tile;

            if (summonPieceDefinition != null)
            {
                if (string.IsNullOrWhiteSpace(displayName))
                    displayName = summonPieceDefinition.displayName;

                if (art == null)
                    art = summonPieceDefinition.icon;
            }
        }
    }
}