using UnityEngine;

namespace Card
{
    // Relative weights (not percentages - they're normalized against whatever total ends up on
    // screen) for rolling a card reward's rarity. A separate asset per difficulty tier is how this
    // is meant to flex for ascensions later: swap the asset a VictoryRewardPanel points at (or add
    // more tiers here) to make Rare harder to roll without touching the picking code itself.
    [CreateAssetMenu(menuName = "Cards/Rarity Odds", fileName = "CardRarityOdds")]
    public class CardRarityOdds : ScriptableObject
    {
        [Header("Reward Roll Weights")]
        public float commonWeight = 70f;
        public float uncommonWeight = 25f;
        public float rareWeight = 5f;

        public float GetWeight(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Common: return Mathf.Max(0f, commonWeight);
                case CardRarity.Uncommon: return Mathf.Max(0f, uncommonWeight);
                case CardRarity.Rare: return Mathf.Max(0f, rareWeight);
                default: return 0f; // Blight/Curse never roll as a normal reward
            }
        }
    }
}
