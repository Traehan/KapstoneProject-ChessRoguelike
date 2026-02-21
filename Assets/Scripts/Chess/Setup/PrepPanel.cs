// Assets/Scripts/Prep/PrepPanel.cs
using UnityEngine;
using UnityEngine.UI;
using Chess;
using Card;

public class PrepPanel : MonoBehaviour
{
    [Header("CardSystem")]
    [SerializeField] private DeckManager deckManager;

    [Header("UI")]
    public Transform gridParent;
    public GameObject iconPrefab;
    public Button confirmButton;
    public Button resetButton;
    public Button undoButton;

    [Header("Placement")]
    public PlacementManager placementManager;

    void Start()
    {
        Debug.Log("DeckManager ref: " + deckManager);
        Debug.Log("PrepHand count: " + (deckManager != null ? deckManager.PrepHand.Count : -1));

        foreach (var card in deckManager.PrepHand)
        {
            SpawnOneIcon(card);
        }

        confirmButton.onClick.AddListener(OnConfirm);
    }

    void SpawnOneIcon(Card.Card card)
    {
        var def = card.Definition;

        var prefabToUse = def.iconPrefabOverride != null 
            ? def.iconPrefabOverride 
            : iconPrefab;

        if (prefabToUse == null)
        {
            Debug.LogError("[PrepPanel] Icon Prefab is missing.");
            return;
        }

        var go = Instantiate(prefabToUse, gridParent);
        var icon = go.GetComponent<DraggablePieceIcon>();
        icon.Init(card, placementManager, this);
    }

    void OnConfirm()
    {
        TurnManager.Instance?.BeginEncounterFromPreparation();
        gameObject.SetActive(false);
    }

    public void OnIconConsumed(DraggablePieceIcon icon)
    {
        Destroy(icon.gameObject);
    }

    void SpawnAllIcons()
    {
        for (int i = gridParent.childCount - 1; i >= 0; i--)
            Destroy(gridParent.GetChild(i).gameObject);

        foreach (var card in deckManager.PrepHand)
        {
            SpawnOneIcon(card);
        }
    }

    public void OnReset()
    {
        placementManager.ResetAll();
        SpawnAllIcons();
    }

    public void OnUndo()
    {
        if (placementManager.UndoLast(out var card))
        {
            SpawnOneIcon(card);
        }
    }
}