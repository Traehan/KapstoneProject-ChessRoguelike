using UnityEngine;

namespace Chess
{
    // Lives permanently in the persistent run UI scene, showing every relic the player owns.
    // Deliberately has no cross-scene references - it only listens to GameSession's event, so it
    // works no matter which scene granted the relic (relic selection, an elite fight, a boss, ...).
    public class RelicBarUI : MonoBehaviour
    {
        [SerializeField] Transform container;
        [SerializeField] RelicIconUI iconPrefab;

        void OnEnable()
        {
            if (GameSession.I != null)
                GameSession.I.OnRelicAdded += HandleRelicAdded;

            RefreshAll();
        }

        void OnDisable()
        {
            if (GameSession.I != null)
                GameSession.I.OnRelicAdded -= HandleRelicAdded;
        }

        void HandleRelicAdded(RelicSO relic) => SpawnIcon(relic);

        void RefreshAll()
        {
            if (container == null || GameSession.I == null) return;

            for (int i = container.childCount - 1; i >= 0; i--)
                Destroy(container.GetChild(i).gameObject);

            foreach (var relic in GameSession.I.ownedRelics)
                SpawnIcon(relic);
        }

        void SpawnIcon(RelicSO relic)
        {
            if (container == null || iconPrefab == null || relic == null) return;

            var icon = Instantiate(iconPrefab, container);
            icon.Bind(relic);
        }
    }
}
