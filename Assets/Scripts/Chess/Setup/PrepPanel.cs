// Assets/Scripts/Prep/PrepPanel.cs
using UnityEngine;
using UnityEngine.UI;
using Chess;
using Card;
using GameManager;

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
        if (GameSession.I == null)
        {
            Debug.LogError("[PrepPanel] GameSession.I is null.");
            return;
        }

        SpawnAllIcons();

        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
        if (resetButton != null) resetButton.onClick.AddListener(OnReset);
        if (undoButton != null) undoButton.onClick.AddListener(OnUndo);
    }

    void SpawnOneIcon(PieceDefinition def)
    {
        if (def == null) return;

        if (iconPrefab == null)
        {
            Debug.LogError("[PrepPanel] iconPrefab is missing. Assign your generic card prefab.");
            return;
        }

        var go = Instantiate(iconPrefab, gridParent);

        var view = go.GetComponent<CardView>();
        if (view != null)
        {
            // Prep still works from PieceDefinition, so wrap it in a temporary compatibility Card.
            var tempCard = new Card.Card(def, manaCost: 0);
            view.Bind(tempCard);
        }

        var icon = go.GetComponent<DraggablePieceIcon>();
        if (icon == null)
        {
            Debug.LogError("[PrepPanel] iconPrefab is missing DraggablePieceIcon.");
            return;
        }

        icon.Init(def, placementManager, this);
    }

    void SpawnAllIcons()
    {
        if (gridParent == null)
        {
            Debug.LogError("[PrepPanel] gridParent is not assigned.");
            return;
        }

        for (int i = gridParent.childCount - 1; i >= 0; i--)
            Destroy(gridParent.GetChild(i).gameObject);

        if (GameSession.I == null) return;
        if (GameSession.I.CurrentArmy == null) return;

        foreach (var def in GameSession.I.CurrentArmy)
            SpawnOneIcon(def);
    }

    void OnConfirm()
    {
        TurnManager.Instance?.BeginEncounterFromPreparation();
        gameObject.SetActive(false);
        SceneController.instance.LoadAdditive("UI_Battle");
    }

    public void OnIconConsumed(DraggablePieceIcon icon)
    {
        if (icon != null)
            Destroy(icon.gameObject);
    }

    public void OnReset()
    {
        if (placementManager != null)
            placementManager.ResetAll();

        SpawnAllIcons();
    }

    public void OnUndo()
    {
        if (placementManager != null && placementManager.UndoLast(out var def))
            SpawnOneIcon(def);
    }
}