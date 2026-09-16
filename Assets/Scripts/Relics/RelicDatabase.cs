using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Chess
{
    [CreateAssetMenu(menuName = "Relics/Relic Database")]
    public class RelicDatabase : ScriptableObject
    {
        [Tooltip("Every relic in the game - general and clan-exclusive alike. Pools are filtered from this at draw time.")]
        public List<RelicSO> allRelics = new();

        public List<RelicSO> GetPool(RelicRarity rarity, ClanDefinition clan)
        {
            return allRelics
                .Where(r => r != null && r.rarity == rarity && r.IsAvailableFor(clan))
                .ToList();
        }

        public List<RelicSO> DrawRandom(RelicRarity rarity, ClanDefinition clan, int count, IEnumerable<RelicSO> exclude = null)
        {
            var pool = GetPool(rarity, clan);

            if (exclude != null)
            {
                var excludeSet = new HashSet<RelicSO>(exclude);
                pool.RemoveAll(excludeSet.Contains);
            }

            for (int i = 0; i < pool.Count; i++)
            {
                int rand = Random.Range(i, pool.Count);
                (pool[i], pool[rand]) = (pool[rand], pool[i]);
            }

            return pool.Take(Mathf.Max(0, count)).ToList();
        }
    }
}
