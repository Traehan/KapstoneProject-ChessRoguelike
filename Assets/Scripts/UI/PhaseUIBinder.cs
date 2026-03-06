using TMPro;
using UnityEngine;

namespace Chess
{
    public class PhaseUIBinder : MonoBehaviour
    {
        [Header("Shared Meter UI")] [SerializeField]
        GameObject meterRoot; // the circle UI (ALWAYS ON)

        [SerializeField] TMP_Text meterText; // "3/10" or "2/3"
        [SerializeField] TMP_Text meterLabel; // optional: "MANA" vs "AP"

        [Header("Hand Panel")] [SerializeField]
        GameObject handPanelRoot;

        [Header("End Turn Button Label")] [SerializeField]
        TMP_Text endTurnButtonLabel;

        void Start()
        {
            var tm = TurnManager.Instance;
            if (tm == null || tm.Phase != TurnPhase.SpellPhase) return;
        
            int current = tm.CurrentMana;
            int max = tm.MaxMana;
        
            if (meterText != null)
                meterText.text = $"{current}/{max}";
                tm.OnManaChanged += HandleManaChanged;
        }

        void OnEnable()
        {
            var tm = TurnManager.Instance;
            if (tm == null) return;

            tm.OnPhaseChanged += HandlePhaseChanged;
            tm.OnManaChanged += HandleManaChanged;
            GameEvents.OnAPChanged += HandleAPChanged;

            // ensure meter stays visible
            if (meterRoot != null) meterRoot.SetActive(true);

            // initialize
            HandlePhaseChanged(tm.Phase);
            HandleManaChanged(tm.CurrentMana, tm.MaxMana);
        }

        void OnDisable()
        {
            var tm = TurnManager.Instance;
            if (tm == null) return;

            tm.OnPhaseChanged -= HandlePhaseChanged;
            GameEvents.OnManaChanged -= HandleManaChanged;
            GameEvents.OnAPChanged -= HandleAPChanged;
        }

        void HandlePhaseChanged(TurnPhase phase)
        {
            bool isSpell = phase == TurnPhase.SpellPhase;
            bool isPlayer = phase == TurnPhase.PlayerTurn;

            // Hand only in SpellPhase
            if (handPanelRoot != null) handPanelRoot.SetActive(isSpell);

            // Meter always visible, but label changes
            if (meterRoot != null) meterRoot.SetActive(true);

            if (meterLabel != null)
            {
                if (isSpell) meterLabel.text = "MANA";
                else if (isPlayer) meterLabel.text = "AP";
                else if (phase == TurnPhase.EnemyTurn) meterLabel.text = "...";
                else meterLabel.text = "";
            }

            if (endTurnButtonLabel != null)
            {
                if (isSpell) endTurnButtonLabel.text = "START\nATTACK";
                else if (isPlayer) endTurnButtonLabel.text = "END\nTURN";
                else if (phase == TurnPhase.EnemyTurn) endTurnButtonLabel.text = "ENEMY\nTURN";
                else endTurnButtonLabel.text = "…";
            }

            // NOTE: meter value itself updates via events below.
            // But when switching phases, you want it to show the right current value:
            var tm = TurnManager.Instance;
            if (tm == null) return;

            if (isSpell)
            {
                // show mana immediately
                if (meterText != null) meterText.text = $"{tm.CurrentMana}/{tm.MaxMana}";
            }
            // For AP, we wait for GameEvents.OnAPChanged at BeginPlayerTurn,
            // OR you can set it here if you have access to current/max AP.
        }

        void HandleManaChanged(int current, int max)
        {
            // Only write to shared meter if currently in SpellPhase
            var tm = TurnManager.Instance;
            if (tm == null || tm.Phase != TurnPhase.SpellPhase) return;
            

            if (meterText != null)
                meterText.text = $"{current}/{max}";
        }

        void HandleAPChanged(int current, int max)
        {
            // Only write to shared meter if currently in PlayerTurn
            var tm = TurnManager.Instance;
            if (tm == null || tm.Phase != TurnPhase.PlayerTurn) return;

            if (meterText != null)
                meterText.text = $"{current}/{max}";
        }

        
    }
}