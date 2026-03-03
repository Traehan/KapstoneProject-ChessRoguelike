// Assets/Scripts/Map/StartTroopPopup.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Chess;

public class StartTroopPopup : MonoBehaviour
{
    [Header("UI")]
    public GameObject panel;      // root panel
    public Image pieceImage;      // optional – shows icon if provided
    public TextMeshProUGUI title; // e.g., "Starting Troop Granted!"
    public TextMeshProUGUI desc;  // e.g., "You received: Rook"
    public Button closeButton;    // top-right X

    void Start()
    {
        panel.SetActive(false);
        closeButton.onClick.AddListener(Hide);

        TryShowGrant();
    }

    void Update()
    {
        if (panel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            Hide();
    }

    void TryShowGrant()
    {
        if (GameSession.I == null) return;
        if (GameSession.I.selectedClan == null) return;
        if (GameSession.I.selectedClan.queenPrefab == null) return;

        PieceDefinition troop = null;

        foreach (var def in GameSession.I.CurrentArmy)
        {
            if (def == null) continue;

            // Skip the queen by prefab identity
            if (def.piecePrefab == GameSession.I.selectedClan.queenPrefab)
                continue;

            troop = def;
            break;
        }

        if (troop == null) return;

        if (title) title.text = "Starting Troop Granted";
        if (desc) desc.text = $"You received: {troop.displayName}";
        if (pieceImage) pieceImage.sprite = troop.icon;

        panel.SetActive(true);
    }

    void Hide() => panel.SetActive(false);
}