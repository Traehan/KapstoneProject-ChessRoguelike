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
    public GameObject iconPrefab; // MUST be CardIcon_generic
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

        // ALWAYS use the generic card prefab (NO iconPrefabOverride)
        if (iconPrefab == null)
        {
            Debug.LogError("[PrepPanel] iconPrefab is missing. Assign CardIcon_generic.");
            return;
        }

        var go = Instantiate(iconPrefab, gridParent);

        // Bind visuals + stats
        var view = go.GetComponent<CardView>();
        if (view != null)
            view.Bind(def, apCost: 0);

        var icon = go.GetComponent<DraggablePieceIcon>();
        if (icon == null)
        {
            Debug.LogError("[PrepPanel] CardIcon_generic is missing DraggablePieceIcon.");
            return;
        }

        icon.Init(def, placementManager, this);
    }

    void SpawnAllIcons()
    {
        for (int i = gridParent.childCount - 1; i >= 0; i--)
            Destroy(gridParent.GetChild(i).gameObject);

        if (GameSession.I == null) return;

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
        Destroy(icon.gameObject);
    }

    public void OnReset()
    {
        placementManager.ResetAll();
        SpawnAllIcons();
    }

    public void OnUndo()
    {
        if (placementManager.UndoLast(out var def))
            SpawnOneIcon(def);
    }
}