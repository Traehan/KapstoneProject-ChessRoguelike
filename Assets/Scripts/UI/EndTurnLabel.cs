using TMPro;
using UnityEngine;

namespace Chess
{
    public class EndTurnLabel : MonoBehaviour
    {
        [SerializeField] TMP_Text label;

        void Start()
        {
            TurnManager.Instance.OnPhaseChanged += UpdateLabel;
            UpdateLabel(TurnManager.Instance.Phase);
        }

        void UpdateLabel(TurnPhase phase)
        {
            if (phase == TurnPhase.SpellPhase)
                label.text = "Start Movement";
            else if (phase == TurnPhase.PlayerTurn)
                label.text = "End Turn";
        }
    }
}