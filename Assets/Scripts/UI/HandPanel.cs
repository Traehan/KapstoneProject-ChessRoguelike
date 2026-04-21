using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Chess;
using Card;

public class HandPanel : MonoBehaviour
{
    [Header("Refs")]
    public DeckManager deckManager;
    public PlacementManager placementManager;

    [Header("UI")]
    public RectTransform handCardsRoot;
    public GameObject iconPrefab;
    public RectTransform deckAnchor;
    public RectTransform discardAnchor;

    [Header("Layout")]
    [SerializeField] float cardSpacing = 150f;
    [SerializeField] float drawDuration = 0.45f;
    [SerializeField] float discardDuration = 0.30f;
    [SerializeField] float relayoutDuration = 0.18f;
    [SerializeField] float drawStagger = 0.12f;
    [SerializeField] float discardStagger = 0.08f;
    [SerializeField] float discardScale = 0.25f;
    [SerializeField] Ease drawEase = Ease.OutCubic;
    [SerializeField] Ease discardEase = Ease.InBack;
    [SerializeField] Ease relayoutEase = Ease.OutCubic;

    readonly Dictionary<string, HandCardUI> _visualsById = new();
    readonly List<Card.Card> _orderedHandCards = new();
    readonly Queue<Card.Card> _drawQueue = new();
    readonly HashSet<string> _discardAnimatingIds = new();

    bool _processingDrawQueue;
    bool _bulkDiscardInProgress;
    bool _suppressIndividualDiscardAnimations;

    void Awake()
    {
        if (deckManager == null) deckManager = FindObjectOfType<DeckManager>();
        if (placementManager == null) placementManager = FindObjectOfType<PlacementManager>();
    }

    void OnEnable()
    {
        if (deckManager == null) deckManager = FindObjectOfType<DeckManager>();
        if (placementManager == null) placementManager = FindObjectOfType<PlacementManager>();

        GameEvents.OnCardDrawn += HandleCardDrawn;
        GameEvents.OnCardDiscarded += HandleCardDiscarded;
        GameEvents.OnCardExhausted += HandleCardExhausted;
        GameEvents.OnCardRemovedFromHand += HandleCardRemovedFromHand;
        GameEvents.OnCardReturnedToHand += HandleCardReturnedToHand;
        GameEvents.OnCardPlayed += HandleCardPlayed;
        GameEvents.OnPhaseChanged += HandlePhaseChanged;
    }

    void OnDisable()
    {
        GameEvents.OnCardDrawn -= HandleCardDrawn;
        GameEvents.OnCardDiscarded -= HandleCardDiscarded;
        GameEvents.OnCardExhausted -= HandleCardExhausted;
        GameEvents.OnCardRemovedFromHand -= HandleCardRemovedFromHand;
        GameEvents.OnCardReturnedToHand -= HandleCardReturnedToHand;
        GameEvents.OnCardPlayed -= HandleCardPlayed;
        GameEvents.OnPhaseChanged -= HandlePhaseChanged;
    }

    // Keep this only as a manual recovery/debug fallback.
    public void RebuildHand()
    {
        FullSyncFromDeckManagerImmediate();
    }

    public void PlayBulkDiscardSequenceAndHide()
    {
        if (_bulkDiscardInProgress)
            return;

        StartCoroutine(BulkDiscardRoutine());
    }

    IEnumerator BulkDiscardRoutine()
    {
        _bulkDiscardInProgress = true;
        _suppressIndividualDiscardAnimations = true;

        var visuals = _orderedHandCards
            .Select(GetVisualForCard)
            .Where(v => v != null)
            .ToList();

        visuals.Sort((a, b) => b.RectTransform.GetSiblingIndex().CompareTo(a.RectTransform.GetSiblingIndex()));

        for (int i = 0; i < visuals.Count; i++)
        {
            string runtimeId = visuals[i].Card != null ? visuals[i].Card.RuntimeId : null;
            if (!string.IsNullOrEmpty(runtimeId))
                _discardAnimatingIds.Add(runtimeId);

            AnimateToDiscardAndDestroy(visuals[i], discardDuration, runtimeId);
            yield return new WaitForSecondsRealtime(discardStagger);
        }

        yield return new WaitForSecondsRealtime(discardDuration + 0.05f);

        _orderedHandCards.Clear();
        _visualsById.Clear();
        _discardAnimatingIds.Clear();

        _suppressIndividualDiscardAnimations = false;
        _bulkDiscardInProgress = false;

        gameObject.SetActive(false);
    }

    void HandleCardDrawn(Card.Card card)
    {
        if (card == null)
            return;

        if (_visualsById.ContainsKey(card.RuntimeId))
            return;

        _drawQueue.Enqueue(card);

        if (!_processingDrawQueue)
            StartCoroutine(ProcessDrawQueue());
    }

    IEnumerator ProcessDrawQueue()
    {
        _processingDrawQueue = true;

        while (_drawQueue.Count > 0)
        {
            var card = _drawQueue.Dequeue();
            if (card == null)
                continue;

            if (_visualsById.ContainsKey(card.RuntimeId))
                continue;

            CreateVisualAtDeck(card);
            UpdateOrderedHandFromDeckManager();
            LayoutHandAnimated(drawDuration);

            yield return new WaitForSecondsRealtime(drawStagger);
        }

        _processingDrawQueue = false;
    }

    void HandleCardDiscarded(Card.Card card)
    {
        if (card == null)
            return;

        if (_suppressIndividualDiscardAnimations)
            return;

        TryAnimateDiscard(card, discardDuration);
    }

    void HandleCardExhausted(Card.Card card)
    {
        if (card == null)
            return;

        TryAnimateDiscard(card, discardDuration * 0.9f);
    }

    void HandleCardRemovedFromHand(Card.Card card)
    {
        if (card == null)
            return;

        RemoveCardFromOrder(card);
    }

    void HandleCardReturnedToHand(Card.Card card)
    {
        if (card == null)
            return;

        if (_visualsById.ContainsKey(card.RuntimeId))
            return;

        CreateVisualImmediate(card);
        UpdateOrderedHandFromDeckManager();
        LayoutHandImmediate();
    }

    void HandleCardPlayed(Card.Card card)
    {
        if (card == null)
            return;

        // Unit cards leave hand quickly when played.
        if (card.IsUnitCard())
        {
            var visual = GetVisualForCard(card);
            if (visual == null)
                return;

            string runtimeId = card.RuntimeId;
            _discardAnimatingIds.Add(runtimeId);

            RemoveCardFromOrder(card);
            AnimateToDiscardAndDestroy(visual, discardDuration * 0.9f, runtimeId);
            LayoutHandAnimated(relayoutDuration);
        }
    }

    void HandlePhaseChanged(TurnPhase phase)
    {
        if (phase == TurnPhase.SpellPhase)
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
        }
    }

    bool TryAnimateDiscard(Card.Card card, float duration)
    {
        if (card == null)
            return false;

        if (_discardAnimatingIds.Contains(card.RuntimeId))
            return true;

        var visual = GetVisualForCard(card);
        if (visual == null)
            return false;

        _discardAnimatingIds.Add(card.RuntimeId);

        RemoveCardFromOrder(card);
        AnimateToDiscardAndDestroy(visual, duration, card.RuntimeId);
        LayoutHandAnimated(relayoutDuration);
        return true;
    }

    void CreateVisualAtDeck(Card.Card card)
    {
        if (iconPrefab == null || handCardsRoot == null)
            return;

        GameObject go = Instantiate(iconPrefab, handCardsRoot);
        go.transform.SetAsLastSibling();

        var visual = BuildVisual(go, card);
        if (visual == null)
            return;

        if (deckAnchor != null)
            visual.RectTransform.position = deckAnchor.position;

        visual.CanvasGroup.alpha = 0.95f;
        _visualsById[card.RuntimeId] = visual;
    }

    void CreateVisualImmediate(Card.Card card)
    {
        if (iconPrefab == null || handCardsRoot == null)
            return;

        GameObject go = Instantiate(iconPrefab, handCardsRoot);
        go.transform.SetAsLastSibling();

        var visual = BuildVisual(go, card);
        if (visual == null)
            return;

        visual.CanvasGroup.alpha = 1f;
        _visualsById[card.RuntimeId] = visual;
    }

    HandCardUI BuildVisual(GameObject go, Card.Card card)
    {
        if (go == null || card == null)
            return null;

        var rect = go.GetComponent<RectTransform>();
        var canvasGroup = go.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = go.AddComponent<CanvasGroup>();

        var view = go.GetComponent<CardView>();
        if (view == null)
            view = go.GetComponentInChildren<CardView>();

        if (view != null)
            view.Bind(card);

        var summonDef = card.GetSummonPieceDefinition();

        if (card.IsUnitCard() && summonDef != null)
        {
            var icon = go.GetComponent<DraggablePieceIcon>();
            if (icon != null)
                icon.InitForCombat(card, placementManager, this, deckManager);

            var spellButtonOld = go.GetComponent<SpellCardButton>();
            if (spellButtonOld != null)
                spellButtonOld.enabled = false;
        }
        else if (card.IsSpellCard())
        {
            var icon = go.GetComponent<DraggablePieceIcon>();
            if (icon != null)
                icon.enabled = false;

            var spellButton = go.GetComponent<SpellCardButton>();
            if (spellButton == null)
                spellButton = go.AddComponent<SpellCardButton>();

            spellButton.Init(card);
        }

        return new HandCardUI
        {
            Card = card,
            GameObject = go,
            RectTransform = rect,
            CanvasGroup = canvasGroup,
            CardView = view
        };
    }

    void AnimateToDiscardAndDestroy(HandCardUI visual, float duration, string runtimeId)
    {
        if (visual == null || visual.GameObject == null)
            return;

        if (visual.RectTransform != null)
            visual.RectTransform.DOKill();

        if (visual.CanvasGroup != null)
            visual.CanvasGroup.DOKill();

        Vector3 discardPos = discardAnchor != null ? discardAnchor.position : visual.RectTransform.position;

        Sequence seq = DOTween.Sequence();
        seq.Join(visual.RectTransform.DOMove(discardPos, duration).SetEase(discardEase));
        seq.Join(visual.RectTransform.DOScale(discardScale, duration).SetEase(discardEase));
        seq.Join(visual.CanvasGroup.DOFade(0.25f, duration));
        seq.OnComplete(() =>
        {
            if (!string.IsNullOrEmpty(runtimeId))
                _discardAnimatingIds.Remove(runtimeId);

            if (visual.Card != null)
                _visualsById.Remove(visual.Card.RuntimeId);

            if (visual.GameObject != null)
                Destroy(visual.GameObject);
        });
    }

    void LayoutHandAnimated(float duration)
    {
        for (int i = 0; i < _orderedHandCards.Count; i++)
        {
            var card = _orderedHandCards[i];
            var visual = GetVisualForCard(card);
            if (visual == null)
                continue;

            Vector2 target = GetAnchoredPositionForIndex(i, _orderedHandCards.Count);

            if (visual.RectTransform != null)
            {
                visual.RectTransform.DOKill();
                visual.RectTransform.DOAnchorPos(target, duration).SetEase(relayoutEase);
            }

            if (visual.CanvasGroup != null)
            {
                visual.CanvasGroup.DOKill();
                visual.CanvasGroup.DOFade(1f, Mathf.Min(duration, relayoutDuration));
            }
        }
    }

    void LayoutHandImmediate()
    {
        for (int i = 0; i < _orderedHandCards.Count; i++)
        {
            var card = _orderedHandCards[i];
            var visual = GetVisualForCard(card);
            if (visual == null)
                continue;

            visual.RectTransform.anchoredPosition = GetAnchoredPositionForIndex(i, _orderedHandCards.Count);
            visual.CanvasGroup.alpha = 1f;
        }
    }

    Vector2 GetAnchoredPositionForIndex(int index, int count)
    {
        float totalWidth = Mathf.Max(0, count - 1) * cardSpacing;
        float startX = -totalWidth * 0.5f;
        return new Vector2(startX + index * cardSpacing, 0f);
    }

    void FullSyncFromDeckManagerImmediate()
    {
        ClearAllVisuals();

        if (deckManager == null || handCardsRoot == null)
            return;

        _orderedHandCards.Clear();

        foreach (var card in deckManager.Hand)
        {
            if (card == null)
                continue;

            _orderedHandCards.Add(card);
            CreateVisualImmediate(card);
        }

        LayoutHandImmediate();
    }

    void UpdateOrderedHandFromDeckManager()
    {
        _orderedHandCards.Clear();

        if (deckManager == null)
            return;

        foreach (var card in deckManager.Hand)
        {
            if (card != null)
                _orderedHandCards.Add(card);
        }
    }

    void RemoveCardFromOrder(Card.Card card)
    {
        if (card == null)
            return;

        for (int i = _orderedHandCards.Count - 1; i >= 0; i--)
        {
            if (_orderedHandCards[i] != null && _orderedHandCards[i].RuntimeId == card.RuntimeId)
            {
                _orderedHandCards.RemoveAt(i);
                return;
            }
        }
    }

    HandCardUI GetVisualForCard(Card.Card card)
    {
        if (card == null)
            return null;

        _visualsById.TryGetValue(card.RuntimeId, out var visual);
        return visual;
    }

    void ClearAllVisuals()
    {
        foreach (var pair in _visualsById)
        {
            if (pair.Value != null && pair.Value.GameObject != null)
            {
                if (pair.Value.RectTransform != null)
                    pair.Value.RectTransform.DOKill();

                if (pair.Value.CanvasGroup != null)
                    pair.Value.CanvasGroup.DOKill();

                Destroy(pair.Value.GameObject);
            }
        }

        _visualsById.Clear();
        _orderedHandCards.Clear();
        _drawQueue.Clear();
        _discardAnimatingIds.Clear();

        if (handCardsRoot == null)
            return;

        for (int i = handCardsRoot.childCount - 1; i >= 0; i--)
        {
            var child = handCardsRoot.GetChild(i);
            if (child == null) continue;

            var rect = child as RectTransform;
            if (rect != null)
                rect.DOKill();

            Destroy(child.gameObject);
        }
    }

    public void OnCardPlayed(DraggablePieceIcon icon)
    {
        if (icon != null)
            Destroy(icon.gameObject);
    }

    class HandCardUI
    {
        public Card.Card Card;
        public GameObject GameObject;
        public RectTransform RectTransform;
        public CanvasGroup CanvasGroup;
        public CardView CardView;
    }
}