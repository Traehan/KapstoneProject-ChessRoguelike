using UnityEngine;

namespace Chess
{
    // A relic is a run-persistent AbilitySO: same lifecycle hooks clan queen abilities use
    // (OnBeginPlayerTurn, OnEndPlayerTurn, OnPieceMoved, OnAttackResolved, OnStatusApplied, ...),
    // but owned by the run (GameSession.ownedRelics) instead of baked into a ClanDefinition.
    // TurnManager folds owned relics into the same _abilities list it already notifies, so a new
    // relic only needs to override the hook(s) it cares about - no new wiring required.
    public abstract class RelicSO : AbilitySO
    {
        [Header("Relic Info")]
        public string displayName;
        public Sprite icon;
        public RelicRarity rarity = RelicRarity.Common;

        [Tooltip("Leave empty for a general relic available to any clan. Assign a clan to restrict this relic to that clan's pool.")]
        public ClanDefinition restrictedToClan;

        public bool IsAvailableFor(ClanDefinition clan)
            => restrictedToClan == null || restrictedToClan == clan;
    }
}
