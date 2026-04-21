using UnityEngine;
using TMPro;
using Card;

public class DeckViewOpenButton : MonoBehaviour
{
    public enum ViewType
    {
        RunDeck = 0,
        BattleDeck = 1,
        BattleDiscard = 2
    }

    [SerializeField] ViewType viewType = ViewType.RunDeck;
    [SerializeField] string panelTitle = "Deck";
    [SerializeField] GameObject movementPanel;

    [Header("Count Label")]
    [SerializeField] TMP_Text buttonText;
    [SerializeField] bool showCountOnButton = true;

    void OnEnable()
    {
        Chess.GameEvents.OnCardDrawn += HandleCardPileChanged;
        Chess.GameEvents.OnCardDiscarded += HandleCardPileChanged;
        Chess.GameEvents.OnCardAddedToHand += HandleCardPileChanged;
        Chess.GameEvents.OnCardRemovedFromHand += HandleCardPileChanged;
        Chess.GameEvents.OnCardReturnedToHand += HandleCardPileChanged;
        Chess.GameEvents.OnCardExhausted += HandleCardPileChanged;
        Chess.GameEvents.OnCardPlayed += HandleCardPileChanged;
        Chess.GameEvents.OnPhaseChanged += HandlePhaseChanged;

        RefreshButtonLabel();
    }

    void OnDisable()
    {
        Chess.GameEvents.OnCardDrawn -= HandleCardPileChanged;
        Chess.GameEvents.OnCardDiscarded -= HandleCardPileChanged;
        Chess.GameEvents.OnCardAddedToHand -= HandleCardPileChanged;
        Chess.GameEvents.OnCardRemovedFromHand -= HandleCardPileChanged;
        Chess.GameEvents.OnCardReturnedToHand -= HandleCardPileChanged;
        Chess.GameEvents.OnCardExhausted -= HandleCardPileChanged;
        Chess.GameEvents.OnCardPlayed -= HandleCardPileChanged;
        Chess.GameEvents.OnPhaseChanged -= HandlePhaseChanged;
    }

    public void OpenDeckView()
    {
        if (DeckViewController.Instance == null)
        {
            Debug.LogWarning("[DeckViewOpenButton] No DeckViewController in scene.");
            return;
        }

        switch (viewType)
        {
            case ViewType.RunDeck:
                DeckViewController.Instance.OpenRunDeckView(panelTitle);
                break;

            case ViewType.BattleDeck:
                DeckViewController.Instance.OpenBattleDeckView(panelTitle);
                break;

            case ViewType.BattleDiscard:
                DeckViewController.Instance.OpenBattleDiscardView(panelTitle);
                break;
        }

        if (movementPanel != null)
            movementPanel.SetActive(false);
    }

    public void ToggleDeckView()
    {
        if (DeckViewController.Instance == null)
        {
            Debug.LogWarning("[DeckViewOpenButton] No DeckViewController in scene.");
            return;
        }

        switch (viewType)
        {
            case ViewType.RunDeck:
                DeckViewController.Instance.ToggleRunDeckView(panelTitle);
                break;

            case ViewType.BattleDeck:
                DeckViewController.Instance.ToggleBattleDeckView(panelTitle);
                break;

            case ViewType.BattleDiscard:
                DeckViewController.Instance.ToggleBattleDiscardView(panelTitle);
                break;
        }
    }

    public void CloseDeckView()
    {
        if (DeckViewController.Instance == null)
        {
            Debug.LogWarning("[DeckViewOpenButton] No DeckViewController in scene.");
            return;
        }

        DeckViewController.Instance.Close();

        if (movementPanel != null)
            movementPanel.SetActive(true);
    }

    void HandleCardPileChanged(Card.Card _)
    {
        RefreshButtonLabel();
    }

    void HandlePhaseChanged(Chess.TurnPhase _)
    {
        RefreshButtonLabel();
    }

    void RefreshButtonLabel()
    {
        if (!showCountOnButton || buttonText == null)
            return;

        string baseTitle = string.IsNullOrWhiteSpace(panelTitle) ? "Deck" : panelTitle;

        var deckManager = FindObjectOfType<DeckManager>();
        if (deckManager == null)
        {
            buttonText.text = baseTitle;
            return;
        }

        int count = GetCountForCurrentView(deckManager);

        buttonText.text = $"{count}";
    }

    int GetCountForCurrentView(DeckManager deckManager)
    {
        if (deckManager == null)
            return 0;

        switch (viewType)
        {
            case ViewType.BattleDeck:
                return deckManager.DrawPile != null ? deckManager.DrawPile.Count : 0;

            case ViewType.BattleDiscard:
                return deckManager.Discard != null ? deckManager.Discard.Count : 0;

            case ViewType.RunDeck:
                return GameSession.I != null && GameSession.I.CurrentRunDeck != null
                    ? GameSession.I.CurrentRunDeck.Count
                    : 0;

            default:
                return 0;
        }
    }
}