using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Chess;
using Card;

public class VictoryRewardPanel : MonoBehaviour
{
    [Header("Refs")]
    public GameSession gameSession;

    [Header("Reward Rarity Odds")]
    [Tooltip("Relative weights for rolling each reward slot's rarity. Leave unassigned to fall back to hardcoded defaults. Swap this asset (or scale its weights) to make Rare harder to roll once ascensions exist.")]
    [SerializeField] CardRarityOdds rarityOdds;

    [Header("Panel")]
    public GameObject VictoryPanel;

    [Header("Reward Roots")]
    public GameObject Reward1;
    public GameObject Reward2;
    public GameObject Reward3;

    [Header("Card Views")]
    public CardView Reward1View;
    public CardView Reward2View;
    public CardView Reward3View;
    
    [Header("Movement Reward UI")]
    [SerializeField] GameObject movementRewardRoot;
    [SerializeField] MovementRewardIconUI movementRewardIcon1;
    [SerializeField] MovementRewardIconUI movementRewardIcon2;

    [Header("Reveal Timing")]
    [SerializeField] float initialRevealDelay = 1.6f;
    [SerializeField] float rewardPopDuration = 0.32f;
    [SerializeField] float rewardPopStagger = 0.14f;
    [SerializeField] Ease rewardPopEase = Ease.OutBack;

    readonly List<CardDefinitionSO> _rolledRewards = new();
    readonly Dictionary<GameObject, Vector3> _originalScales = new();
    bool _rewardClaimed = false;
    Coroutine _revealRoutine;

    void Awake()
    {
        if (gameSession == null)
            gameSession = GameSession.I != null ? GameSession.I : FindObjectOfType<GameSession>();
    }

    void OnEnable()
    {
        FillInSlots();

        if (_revealRoutine != null)
            StopCoroutine(_revealRoutine);

        _revealRoutine = StartCoroutine(RevealRewardsSequence());
    }

    public void FillInSlots()
    {
        _rewardClaimed = false;
        _rolledRewards.Clear();

        if (gameSession == null)
        {
            Debug.LogError("[VictoryRewardPanel] No GameSession found.");
            SetSlotActive(Reward1, false);
            SetSlotActive(Reward2, false);
            SetSlotActive(Reward3, false);
            return;
        }

        var pool = gameSession.PotentialSpellPool;
        if (pool == null || pool.Count == 0)
        {
            Debug.LogWarning("[VictoryRewardPanel] PotentialSpellPool is empty.");
            SetSlotActive(Reward1, false);
            SetSlotActive(Reward2, false);
            SetSlotActive(Reward3, false);
            return;
        }

        List<CardDefinitionSO> picks = GetRandomUniqueRewards(pool, 3);

        BindRewardSlot(Reward1, Reward1View, picks, 0);
        BindRewardSlot(Reward2, Reward2View, picks, 1);
        BindRewardSlot(Reward3, Reward3View, picks, 2);
        BindMovementRewardIcons();

        // Rewards are bound and active immediately (so their data/click-handlers are ready), but hidden
        // until RevealRewardsSequence pops each one in on its own delayed beat — a couple seconds after
        // the win panel itself is visible, rather than everything slamming onto screen in the same frame.
        PrimeHidden(Reward1);
        PrimeHidden(Reward2);
        PrimeHidden(Reward3);
        PrimeHidden(movementRewardRoot);
    }

    void PrimeHidden(GameObject go)
    {
        if (go == null || !go.activeSelf)
            return;

        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = go.AddComponent<CanvasGroup>();

        cg.DOKill();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;

        if (go.transform is RectTransform rt)
        {
            rt.DOKill();

            if (!_originalScales.TryGetValue(go, out var originalScale))
            {
                originalScale = rt.localScale;
                _originalScales[go] = originalScale;
            }

            rt.localScale = originalScale * 0.6f;
        }
    }

    IEnumerator RevealRewardsSequence()
    {
        float mult = Mathf.Max(0.5f, JuiceSettings.AnimationDurationMultiplier);

        yield return new WaitForSecondsRealtime(initialRevealDelay * mult);

        PopIn(Reward1);
        yield return new WaitForSecondsRealtime(rewardPopStagger * mult);

        PopIn(Reward2);
        yield return new WaitForSecondsRealtime(rewardPopStagger * mult);

        PopIn(Reward3);
        yield return new WaitForSecondsRealtime(rewardPopStagger * mult);

        PopIn(movementRewardRoot);

        _revealRoutine = null;
    }

    void PopIn(GameObject go)
    {
        if (go == null || !go.activeSelf)
            return;

        float duration = rewardPopDuration * Mathf.Max(0.5f, JuiceSettings.AnimationDurationMultiplier);

        var cg = go.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.DOKill();
            cg.blocksRaycasts = true;
            cg.interactable = true;
            cg.DOFade(1f, duration).SetUpdate(true);
        }

        if (go.transform is RectTransform rt)
        {
            rt.DOKill();

            Vector3 targetScale = _originalScales.TryGetValue(go, out var originalScale)
                ? originalScale
                : Vector3.one;

            rt.DOScale(targetScale, duration).SetEase(rewardPopEase).SetUpdate(true);
        }
    }

    List<CardDefinitionSO> GetRandomUniqueRewards(List<CardDefinitionSO> pool, int amount)
    {
        // Blight/Curse cards are granted directly by whatever inflicts them (relics, events), never
        // drafted here - same as Status/Curse cards in Slay the Spire.
        return CardDraftUtility.RollDistinct(
            pool,
            amount,
            rarityOdds,
            r => r != CardRarity.Blight && r != CardRarity.Curse);
    }

    void BindRewardSlot(GameObject slotRoot, CardView cardView, List<CardDefinitionSO> picks, int index)
    {
        if (slotRoot == null)
            return;

        bool hasReward = index < picks.Count && picks[index] != null;
        slotRoot.SetActive(hasReward);

        if (!hasReward)
            return;

        CardDefinitionSO def = picks[index];
        _rolledRewards.Add(def);

        if (cardView == null)
        {
            Debug.LogWarning($"[VictoryRewardPanel] Missing CardView for reward slot index {index}.");
            return;
        }

        Card.Card runtimeCard = new Card.Card(def);
        cardView.Bind(runtimeCard);

        var inspectItem = cardView.GetComponent<VictoryRewardCardItem>();
        if (inspectItem == null)
            inspectItem = cardView.gameObject.AddComponent<VictoryRewardCardItem>();

        inspectItem.Bind(runtimeCard, this, index);
    }
    
    void BindMovementRewardIcons()
    {
        if (gameSession == null)
            return;

        var rewards = gameSession.lastGrantedMapMovementRewards;

        bool hasRewards = rewards != null && rewards.Count > 0;

        if (movementRewardRoot != null)
            movementRewardRoot.SetActive(hasRewards);

        if (!hasRewards)
        {
            if (movementRewardIcon1 != null)
                movementRewardIcon1.gameObject.SetActive(false);

            if (movementRewardIcon2 != null)
                movementRewardIcon2.gameObject.SetActive(false);

            return;
        }

        if (movementRewardIcon1 != null)
        {
            bool hasFirst = rewards.Count > 0;
            movementRewardIcon1.gameObject.SetActive(hasFirst);

            if (hasFirst)
                movementRewardIcon1.Bind(rewards[0]);
        }

        if (movementRewardIcon2 != null)
        {
            bool hasSecond = rewards.Count > 1;
            movementRewardIcon2.gameObject.SetActive(hasSecond);

            if (hasSecond)
                movementRewardIcon2.Bind(rewards[1]);
        }
    }

    void SetSlotActive(GameObject slot, bool active)
    {
        if (slot != null)
            slot.SetActive(active);
    }

    public CardDefinitionSO GetRewardAtIndex(int index)
    {
        if (index < 0 || index >= _rolledRewards.Count)
            return null;

        return _rolledRewards[index];
    }

    public void ClaimReward1() => ClaimRewardAtIndex(0);
    public void ClaimReward2() => ClaimRewardAtIndex(1);
    public void ClaimReward3() => ClaimRewardAtIndex(2);

    public void ClaimRewardAtIndex(int index)
    {
        if (_rewardClaimed)
            return;

        if (gameSession == null)
        {
            Debug.LogError("[VictoryRewardPanel] Cannot claim reward. GameSession is null.");
            return;
        }

        CardDefinitionSO chosenReward = GetRewardAtIndex(index);
        if (chosenReward == null)
        {
            Debug.LogWarning($"[VictoryRewardPanel] No reward found at index {index}.");
            return;
        }

        gameSession.CurrentRunDeck.Add(chosenReward);
        _rewardClaimed = true;

        Debug.Log($"[VictoryRewardPanel] Added reward card to run deck: {chosenReward.name}");

        HideRewardPanel();
    }

    public void HideRewardPanel()
    {
        if (_revealRoutine != null)
        {
            StopCoroutine(_revealRoutine);
            _revealRoutine = null;
        }

        KillRewardTweens(Reward1);
        KillRewardTweens(Reward2);
        KillRewardTweens(Reward3);
        KillRewardTweens(movementRewardRoot);

        if (VictoryPanel != null)
            VictoryPanel.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    void KillRewardTweens(GameObject go)
    {
        if (go == null)
            return;

        go.GetComponent<CanvasGroup>()?.DOKill();
        go.transform.DOKill();
    }
}