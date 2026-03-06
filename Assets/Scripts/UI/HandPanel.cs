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
        if (deckManager == null || gridParent == null)
        {
            Debug.LogError("HandPanel rebuilding failed: missing either deckManager or gridParent");
            return;
        }
            

        // clear old icons
        for (int i = gridParent.childCount - 1; i >= 0; i--)
            Destroy(gridParent.GetChild(i).gameObject);

        
        // spawn one icon per card in hand
        foreach (var def in deckManager.Hand)
        {
            if (def == null) continue;
            if (iconPrefab == null) continue;

            // ALWAYS use the one generic prefab
            var go = Instantiate(iconPrefab, gridParent);

            // Bind visuals from the PieceDefinition
            var view = go.GetComponent<CardView>();
            if (view != null)
                view.Bind(def, apCost: 1); // later you can make AP per piece/card

            // Drag logic
            var icon = go.GetComponent<DraggablePieceIcon>();
            if (icon != null)
                icon.InitForCombat(def, placementManager, this, deckManager);
        }
    }

    public void OnCardPlayed(DraggablePieceIcon icon)
    {
        // remove the UI icon after a successful play
        Destroy(icon.gameObject);
    }
}