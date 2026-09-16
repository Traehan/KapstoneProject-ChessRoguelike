using UnityEngine;

namespace Card
{
    [CreateAssetMenu(menuName = "Cards/Spell Card Definition", fileName = "SpellCardDefinition")]
    public class SpellCardDefinitionSO : CardDefinitionSO
    {
        
        [Header("Spell Logic")]
        public SpellEffectSO[] effects;

        [Header("After Cast")]
        public bool discardOnCast = true;
        public bool exhaustOnCast = false;

        [Header("Spell Metadata")]
        [TextArea] public string designNotes;
        

        private void OnValidate()
        {
            cardType = CardType.Spell;
        }
    }
}