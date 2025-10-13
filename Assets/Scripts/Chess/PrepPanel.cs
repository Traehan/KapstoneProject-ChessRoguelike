// Assets/Scripts/Prep/PrepPanel.cs
using UnityEngine;
using UnityEngine.UI;
using Chess;
using System.Collections.Generic;

public class PrepPanel : MonoBehaviour
{
    [Header("Data")]
    public List<PieceDefinition> options;

    [Header("UI")]
    public Transform gridParent;               // e.g., a GridLayoutGroup
    public GameObject iconPrefab;              // simple Button/Image with DraggablePieceIcon
    public Button confirmButton;

    [Header("Placement")]
    public PlacementManager placementManager;  // ref in Inspector

    void Awake()
    {
        foreach (var def in options)
        {
            for (int i = 0; i < def.count; i++)
            {
                var go = Instantiate(iconPrefab, gridParent);
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
    }

    public void OnIconConsumed(DraggablePieceIcon icon)
    {
        Destroy(icon.gameObject);
    }
}

