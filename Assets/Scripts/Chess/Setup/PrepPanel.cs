// Assets/Scripts/Prep/PrepPanel.cs
using UnityEngine;
using UnityEngine.UI;
using Chess;
using System.Collections.Generic;
using GameManager;

public class PrepPanel : MonoBehaviour
{
    [Header("Data")]
    public List<PieceDefinition> options;
    [Tooltip("If ON, overrides 'options' with GameSession's current army on Start().")]
    public bool useGameSessionArmy = true;


    [Header("UI")]
    public Transform gridParent;               // e.g., a GridLayoutGroup
    public GameObject iconPrefab;              // simple Button/Image with DraggablePieceIcon
    public Button confirmButton;
    
    public Button resetButton;
    public Button undoButton;

    [Header("Placement")]
    public PlacementManager placementManager;  // ref in Inspector

    void Start()
    {
        // Replace the options from GameSession at runtime
        if (useGameSessionArmy && GameSession.I != null)
        {
            options = new List<PieceDefinition>(GameSession.I.CurrentArmy);
            Debug.Log($"[PrepPanel] Pulled {options.Count} pieces from GameSession.");
        }

        // --- build icons ---
        foreach (var def in options)
        {
            for (int i = 0; i < Mathf.Max(1, def.count); i++)
            {
                var prefabToUse = def.iconPrefabOverride != null ? def.iconPrefabOverride : iconPrefab;
                if (prefabToUse == null)
                {
                    Debug.LogError("[PrepPanel] Icon Prefab is missing and no override provided.");
                    continue;
                }

                var go = Instantiate(prefabToUse, gridParent);
                var icon = go.GetComponent<DraggablePieceIcon>();
                icon.Init(def, placementManager, this);
            }
        }

        confirmButton.onClick.AddListener(OnConfirm);
    }


    void OnConfirm()
    {
        // disallow confirming if nothing placed? (optional)
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
        // clear any existing children
        for (int i = gridParent.childCount - 1; i >= 0; i--)
            Destroy(gridParent.GetChild(i).gameObject);

        // spawn fresh icons from definitions
        foreach (var def in options)
            for (int i = 0; i < def.count; i++)
                SpawnOneIcon(def);
    }
    
    void SpawnOneIcon(PieceDefinition def)
    {
        var prefabToUse = def.iconPrefabOverride != null ? def.iconPrefabOverride : iconPrefab;
        var go = Instantiate(prefabToUse, gridParent);
        var icon = go.GetComponent<DraggablePieceIcon>();
        icon.Init(def, placementManager, this);
    }

    
    public void OnReset()
    {
        placementManager.ResetAll();
        SpawnAllIcons();
    }
    
    public void OnUndo()
    {
        if (placementManager.UndoLast(out var def))
        {
            SpawnOneIcon(def);
        }
    }
}

