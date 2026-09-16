using System;
using System.Collections.Generic;
using UnityEngine;

namespace Card
{
    // Shared weighted-by-rarity card picking. Used by both the victory reward roll
    // (VictoryRewardPanel) and the run-start random card draft (GameSession.GrantRandomStartingCards),
    // so there's one place that implements "bucket a pool by rarity, roll weighted picks without
    // replacement" rather than two copies that could drift out of sync.
    public static class CardDraftUtility
    {
        // Returns up to `count` distinct cards from `pool`, each rolled by rarity weight (via `odds`,
        // or a hardcoded 70/25/5 Common/Uncommon/Rare fallback if `odds` is null) among rarities that
        // pass `allowRarity`. Fewer than `count` are returned if the eligible pool runs out.
        public static List<CardDefinitionSO> RollDistinct(
            IEnumerable<CardDefinitionSO> pool,
            int count,
            CardRarityOdds odds,
            Func<CardRarity, bool> allowRarity = null)
        {
            var byRarity = new Dictionary<CardRarity, List<CardDefinitionSO>>();

            foreach (var def in pool)
            {
                if (def == null) continue;
                if (allowRarity != null && !allowRarity(def.rarity)) continue;

                if (!byRarity.TryGetValue(def.rarity, out var list))
                {
                    list = new List<CardDefinitionSO>();
                    byRarity[def.rarity] = list;
                }

                list.Add(def);
            }

            var results = new List<CardDefinitionSO>(Mathf.Max(0, count));

            for (int i = 0; i < count; i++)
            {
                var picked = RollOne(byRarity, odds);
                if (picked == null) break;

                results.Add(picked);
            }

            return results;
        }

        static CardDefinitionSO RollOne(Dictionary<CardRarity, List<CardDefinitionSO>> byRarity, CardRarityOdds odds)
        {
            float totalWeight = 0f;
            foreach (var kv in byRarity)
            {
                if (kv.Value.Count == 0) continue;
                totalWeight += GetWeight(kv.Key, odds);
            }

            if (totalWeight <= 0f)
                return null;

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;
            CardRarity chosenRarity = default;
            bool chosen = false;

            foreach (var kv in byRarity)
            {
                if (kv.Value.Count == 0) continue;

                cumulative += GetWeight(kv.Key, odds);
                if (roll <= cumulative)
                {
                    chosenRarity = kv.Key;
                    chosen = true;
                    break;
                }
            }

            if (!chosen)
                return null;

            var list = byRarity[chosenRarity];
            int index = UnityEngine.Random.Range(0, list.Count);
            var picked = list[index];
            list.RemoveAt(index);

            return picked;
        }

        static float GetWeight(CardRarity rarity, CardRarityOdds odds)
        {
            if (odds != null)
                return odds.GetWeight(rarity);

            switch (rarity)
            {
                case CardRarity.Common: return 70f;
                case CardRarity.Uncommon: return 25f;
                case CardRarity.Rare: return 5f;
                default: return 0f;
            }
        }
    }
}
