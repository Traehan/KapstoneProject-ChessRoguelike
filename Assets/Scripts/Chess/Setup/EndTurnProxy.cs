// EndTurnProxy.cs (attach to the End Turn button or its parent in UI_Battle)
using UnityEngine;

namespace Chess
{
    public class EndTurnProxy : MonoBehaviour
    {
        public void CallEndTurn()
        {
            var tm = TurnManager.Instance;
            if (tm == null) return;

            if (tm.Phase == TurnPhase.SpellPhase)
            {
                tm.EndSpellPhaseButton(); // Spell → Movement
            }
            else if (tm.Phase == TurnPhase.PlayerTurn)
            {
                tm.EndPlayerTurnButton(); // Movement → Enemy
            }
        }
    }
}