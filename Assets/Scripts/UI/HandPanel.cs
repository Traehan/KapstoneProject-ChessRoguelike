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

    void Awake()
    {
        if (deckManager == null) deckManager = FindObjectOfType<DeckManager>();
        if (placementManager == null) placementManager = FindObjectOfType<PlacementManager>();
    }

    void Start()
    {
        RebuildHand();
    }

    public void RebuildHand()
    {
        if (deckManager == null || gridParent == null)
        {
            Debug.LogError("[HandPanel] Rebuild failed: missing deckManager or gridParent.");
            return;
        }

        for (int i = gridParent.childCount - 1; i >= 0; i--)
            Destroy(gridParent.GetChild(i).gameObject);

        foreach (var card in deckManager.Hand)
        {
            if (card == null || iconPrefab == null)
                continue;

            var go = Instantiate(iconPrefab, gridParent);

            var view = go.GetComponent<CardView>();
            if (view != null)
                view.Bind(card);

            var summonDef = card.GetSummonPieceDefinition();

            if (card.IsUnitCard() && summonDef != null)
            {
                var icon = go.GetComponent<DraggablePieceIcon>();
                if (icon != null)
                    icon.InitForCombat(card, placementManager, this, deckManager);

                var spellButtonOld = go.GetComponent<SpellCardButton>();
                if (spellButtonOld != null)
                    spellButtonOld.enabled = false;
            }
            else if (card.IsSpellCard())
            {
                var icon = go.GetComponent<DraggablePieceIcon>();
                if (icon != null)
                    icon.enabled = false;

                var spellButton = go.GetComponent<SpellCardButton>();
                if (spellButton == null)
                    spellButton = go.AddComponent<SpellCardButton>();

                spellButton.Init(card);
            }
        }
    }

    public void OnCardPlayed(DraggablePieceIcon icon)
    {
        if (icon != null)
            Destroy(icon.gameObject);
    }
}