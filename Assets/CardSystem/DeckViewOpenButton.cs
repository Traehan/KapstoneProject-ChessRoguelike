using UnityEngine;
using TMPro;
using Card;
using Chess; // 👈 ADDED

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

    [Header("Controller")]
    [Tooltip("Leave empty to use DeckViewController.Instance (fine when only one controller is loaded). " +
             "Assign the controller in THIS button's own scene when more than one DeckViewController " +
             "coexists (e.g. SampleScene's battle draw/discard controller alongside PersistentUI's RunDeck one), " +
             "so this button always talks to the right one instead of whichever claimed Instance first.")]
    [SerializeField] DeckViewController controller;

    DeckViewController Controller => controller != null ? controller : DeckViewController.Instance;

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
        // 🔊 ADDED
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayGlobal(SoundEventId.UIDeckShuffle);

        var ctrl = Controller;
        if (ctrl == null)
        {
            Debug.LogWarning("[DeckViewOpenButton] No DeckViewController available (assign one directly, or make sure a DeckViewController is loaded).");
            return;
        }

        switch (viewType)
        {
            case ViewType.RunDeck:
                ctrl.OpenRunDeckView(panelTitle);
                break;

            case ViewType.BattleDeck:
                ctrl.OpenBattleDeckView(panelTitle);
                break;

            case ViewType.BattleDiscard:
                ctrl.OpenBattleDiscardView(panelTitle);
                break;
        }

        if (movementPanel != null)
            movementPanel.SetActive(false);
    }

    public void ToggleDeckView()
    {
        // 🔊 ADDED
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayGlobal(SoundEventId.UIDeckShuffle);

        var ctrl = Controller;
        if (ctrl == null)
        {
            Debug.LogWarning("[DeckViewOpenButton] No DeckViewController available (assign one directly, or make sure a DeckViewController is loaded).");
            return;
        }

        switch (viewType)
        {
            case ViewType.RunDeck:
                ctrl.ToggleRunDeckView(panelTitle);
                break;

            case ViewType.BattleDeck:
                ctrl.ToggleBattleDeckView(panelTitle);
                break;

            case ViewType.BattleDiscard:
                ctrl.ToggleBattleDiscardView(panelTitle);
                break;
        }
    }

    public void CloseDeckView()
    {
        var ctrl = Controller;
        if (ctrl == null)
        {
            Debug.LogWarning("[DeckViewOpenButton] No DeckViewController available (assign one directly, or make sure a DeckViewController is loaded).");
            return;
        }

        ctrl.Close();

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