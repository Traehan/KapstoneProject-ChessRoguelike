using UnityEngine;
using Chess;   // make sure this matches your TurnManager namespace

public class VictoryRewardHandler : MonoBehaviour
{
    private TurnManager _turnManager;
    private bool _rewardGivenThisEncounter;

    private void OnEnable()
    {
        _rewardGivenThisEncounter = false;
        TrySubscribe();
    }

    private void Start()
    {
        // In case OnEnable ran before TurnManager.Instance existed
        if (_turnManager == null)
        {
            TrySubscribe();
        }
    }

    private void OnDisable()
    {
        if (_turnManager != null)
        {
            _turnManager.OnPlayerWon -= OnPlayerVictory;
            _turnManager = null;
        }
    }

    private void TrySubscribe()
    {
        var tm = TurnManager.Instance;
        if (tm == null)
        {
            Debug.LogWarning("[VictoryRewardHandler] TurnManager not found; will try again later.");
            return;
        }

        // Defensive: remove before add so we never get duplicate subscriptions
        tm.OnPlayerWon -= OnPlayerVictory;
        tm.OnPlayerWon += OnPlayerVictory;
        _turnManager = tm;

        Debug.Log("[VictoryRewardHandler] Subscribed to TurnManager.OnPlayerWon");
    }

    private void OnPlayerVictory()
    {
        // Guard: only award once per encounter, even if event fires multiple times
        if (_rewardGivenThisEncounter)
        {
            Debug.Log("[VictoryRewardHandler] Duplicate victory event; reward already given, ignoring.");
            return;
        }

        _rewardGivenThisEncounter = true;

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AwardEncounterVictory();
            Debug.Log("[VictoryRewardHandler] Player victory! Coins awarded.");
        }
        else
        {
            Debug.LogWarning("[VictoryRewardHandler] CurrencyManager not found! Cannot award coins.");
        }
    }
}