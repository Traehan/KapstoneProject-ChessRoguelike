using System.Collections.Generic;
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
    [Tooltip("Slots for the run-start random card draft (GameSession.GrantRandomStartingCards). Shown with an 'xN' badge for how many copies were granted.")]
    [SerializeField] CardView randomCard1View;
    [SerializeField] CardView randomCard2View;
    [SerializeField] CardView troopCardView;

    [Header("Relics")]
    [Tooltip("Shown right after this popup is dismissed, once per run, so the player can pick their opening relic knowing their starting troop.")]
    [SerializeField] RelicSelectionUI relicSelectionUI;

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
            title.text = "Run Started!";

        if (instructionText != null)
            instructionText.text = "Your clan begins this run with these cards and troop.";

        var grantedCards = GameSession.I.GrantRandomStartingCards();
        BindRandomCardSlot(randomCard1View, grantedCards, 0);
        BindRandomCardSlot(randomCard2View, grantedCards, 1);

        if (troopCardView != null)
            troopCardView.Bind(_runtimeDisplayCard);

        if (panel != null)
            panel.SetActive(true);

        GameSession.I.hasShownStartingTroopPopup = true;
    }

    void BindRandomCardSlot(CardView view, List<CardDefinitionSO> grantedCards, int index)
    {
        if (view == null)
            return;

        bool hasCard = grantedCards != null && index < grantedCards.Count && grantedCards[index] != null;
        view.gameObject.SetActive(hasCard);

        if (!hasCard)
            return;

        view.Bind(grantedCards[index]);
        view.SetStackCount(GameSession.I.startingDraftCopiesPerPick);
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

        MaybeOfferStartingRelic();
    }

    void MaybeOfferStartingRelic()
    {
        var session = GameSession.I;
        if (session == null) return;
        if (session.hasShownStartingRelicSelection) return;
        if (relicSelectionUI == null) return;

        // Set before Show() so a double-close (e.g. Escape then the button) can't reopen it.
        session.hasShownStartingRelicSelection = true;
        relicSelectionUI.Show(session.selectedClan, null);
    }
}