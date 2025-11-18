// Assets/Scripts/Map/StartTroopPopup.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
        if (GameSession.I.hasGrantedStartingTroop) return;

        var granted = GameSession.I.GrantRandomStartingTroop();
        if (granted == null) return;

        // Populate UI
        if (title) title.text = "Staring Troop Granted";
        if (desc)  desc.text  = $"You received: {granted.displayName}";
        if (pieceImage) pieceImage.sprite = granted.icon;

        panel.SetActive(true);
    }

    void Hide() => panel.SetActive(false);
}