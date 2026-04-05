using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Card;
using Chess;

public class StartTroopPopup : MonoBehaviour
{
    [Header("UI Root")]
    [SerializeField] GameObject panel;
    [SerializeField] Button closeButton;

    [Header("Optional Text")]
    [SerializeField] TMP_Text title;
    [SerializeField] TMP_Text instructionText;

    [Header("Card Display")]
    [SerializeField] CardView troopCardView;

    Card.Card _runtimeDisplayCard;
    PieceDefinition _grantedTroop;

    void Start()
    {
        if (panel != null)
            panel.SetActive(false);

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Hide);
            closeButton.onClick.AddListener(Hide);
        }

        TryShowGrant();
    }

    void Update()
    {
        if (panel != null && panel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            Hide();
    }

    void TryShowGrant()
    {
        if (GameSession.I == null) return;
        if (GameSession.I.selectedClan == null) return;
        if (GameSession.I.selectedClan.queenDefinition == null) return;

        // Already shown once this run? Never show it again.
        if (GameSession.I.hasShownStartingTroopPopup)
        {
            Hide();
            return;
        }

        _grantedTroop = FindGrantedStartingTroop();
        if (_grantedTroop == null)
        {
            Hide();
            return;
        }

        _runtimeDisplayCard = new Card.Card(_grantedTroop, manaCost: 1);

        if (title != null)
            title.text = "Starting Troop Granted";

        if (instructionText != null)
            instructionText.text = "Your clan begins this run with this troop.";

        if (troopCardView != null)
            troopCardView.Bind(_runtimeDisplayCard);

        if (panel != null)
            panel.SetActive(true);

        GameSession.I.hasShownStartingTroopPopup = true;
    }

    PieceDefinition FindGrantedStartingTroop()
    {
        var session = GameSession.I;
        if (session == null)
            return null;

        var queenDef = session.selectedClan != null ? session.selectedClan.queenDefinition : null;
        var queenPrefab = queenDef != null ? queenDef.piecePrefab : null;

        var army = session.CurrentArmy;
        if (army == null)
            return null;

        for (int i = 0; i < army.Count; i++)
        {
            var def = army[i];
            if (def == null)
                continue;

            if (queenPrefab != null && def.piecePrefab == queenPrefab)
                continue;

            return def;
        }

        return null;
    }

    public void Hide()
    {
        if (panel != null)
            panel.SetActive(false);
    }
}