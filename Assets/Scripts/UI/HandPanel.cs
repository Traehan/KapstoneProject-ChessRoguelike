using UnityEngine;
using Chess;
using Card;

public class HandPanel : MonoBehaviour
{
    [Header("Refs")]
    public DeckManager deckManager;
    public PlacementManager placementManager;

    [Header("UI")]
    public Transform gridParent;
    public GameObject iconPrefab;

    void Start()
    {
        RebuildHand();
    }
    void Awake()
    {
        if (deckManager == null) deckManager = FindObjectOfType<DeckManager>();
        if (placementManager == null) placementManager = FindObjectOfType<PlacementManager>();
    }

    public void RebuildHand()
    {
        if (deckManager == null || gridParent == null) return;

        // clear old icons
        for (int i = gridParent.childCount - 1; i >= 0; i--)
            Destroy(gridParent.GetChild(i).gameObject);

        // spawn one icon per card in hand
        foreach (var def in deckManager.Hand)
        {
            if (def == null) continue;

            var prefabToUse = def.iconPrefabOverride != null ? def.iconPrefabOverride : iconPrefab;
            if (prefabToUse == null) continue;

            var go = Instantiate(prefabToUse, gridParent);
            var icon = go.GetComponent<DraggablePieceIcon>();

            // IMPORTANT: we pass HandPanel so icon can notify on success
            icon.InitForCombat(def, placementManager, this, deckManager);
        }
    }

    public void OnCardPlayed(DraggablePieceIcon icon)
    {
        // remove the UI icon after a successful play
        Destroy(icon.gameObject);
    }
}