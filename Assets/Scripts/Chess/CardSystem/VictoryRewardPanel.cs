using System.Collections.Generic;
using UnityEngine;
using Chess;
using Card;

public class VictoryRewardPanel : MonoBehaviour
{
    [Header("Refs")]
    public GameSession gameSession;

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

    readonly List<CardDefinitionSO> _rolledRewards = new();
    bool _rewardClaimed = false;

    void Awake()
    {
        if (gameSession == null)
            gameSession = GameSession.I != null ? GameSession.I : FindObjectOfType<GameSession>();
    }

    void OnEnable()
    {
        FillInSlots();
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
    }

    List<CardDefinitionSO> GetRandomUniqueRewards(List<CardDefinitionSO> pool, int amount)
    {
        List<CardDefinitionSO> workingPool = new();

        foreach (var def in pool)
        {
            if (def != null)
                workingPool.Add(def);
        }

        int countToTake = Mathf.Min(amount, workingPool.Count);
        List<CardDefinitionSO> results = new(countToTake);

        for (int i = 0; i < countToTake; i++)
        {
            int randomIndex = Random.Range(0, workingPool.Count);
            CardDefinitionSO picked = workingPool[randomIndex];
            results.Add(picked);
            workingPool.RemoveAt(randomIndex);
        }

        return results;
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
        if (VictoryPanel != null)
            VictoryPanel.SetActive(false);
        else
            gameObject.SetActive(false);
    }
}