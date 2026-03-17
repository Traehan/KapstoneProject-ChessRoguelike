using UnityEngine;
using Chess;

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
        if (_turnManager == null)
            TrySubscribe();
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
            return;

        tm.OnPlayerWon -= OnPlayerVictory;
        tm.OnPlayerWon += OnPlayerVictory;
        _turnManager = tm;
    }

    private void OnPlayerVictory()
    {
        if (_rewardGivenThisEncounter)
            return;

        _rewardGivenThisEncounter = true;

        var gs = GameSession.I;
        if (gs != null && gs.isBossBattle)
            gs.bossDefeated = true;

        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.AwardEncounterVictory();
    }
}