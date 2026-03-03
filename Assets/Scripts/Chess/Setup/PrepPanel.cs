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
        Debug.Log("DeckManager ref: " + deckManager);

        if (GameSession.I == null)
        {
            Debug.LogError("[PrepPanel] GameSession.I is null.");
            return;
        }

        // Leaders only: Queen + Troop (CurrentArmy should be exactly 2)
        foreach (var def in GameSession.I.CurrentArmy)
        {
            if (def == null) continue;
            SpawnOneIcon(def);
        }

        confirmButton.onClick.AddListener(OnConfirm);
    }

    void SpawnOneIcon(PieceDefinition def)
    {
        var prefabToUse = def.iconPrefabOverride != null ? def.iconPrefabOverride : iconPrefab;
        if (prefabToUse == null)
        {
            Debug.LogError("[PrepPanel] Icon Prefab is missing.");
            return;
        }

        var go = Instantiate(prefabToUse, gridParent);
        var icon = go.GetComponent<DraggablePieceIcon>();
        icon.Init(def, placementManager, this);
        
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

    void SpawnAllIcons()
    {
        for (int i = gridParent.childCount - 1; i >= 0; i--)
            Destroy(gridParent.GetChild(i).gameObject);

        if (GameSession.I == null) return;

        foreach (var def in GameSession.I.CurrentArmy)
        {
            if (def == null) continue;
            SpawnOneIcon(def);
        }
    }

    public void OnReset()
    {
        placementManager.ResetAll();
        SpawnAllIcons();
    }

    public void OnUndo()
    {
        if (placementManager.UndoLast(out var _def))
            SpawnOneIcon(_def);
    }
}