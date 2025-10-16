// EndTurnProxy.cs (attach to the End Turn button or its parent in UI_Battle)
using UnityEngine;

namespace Chess
{
    public class EndTurnProxy : MonoBehaviour
    {
        public void CallEndTurn()
        {
            var tm = TurnManager.Instance ?? FindObjectOfType<TurnManager>();
            if (tm == null) { Debug.LogError("EndTurnProxy: No TurnManager found in any loaded scene."); return; }
            if (!tm.IsPlayerTurn) { Debug.Log("EndTurnProxy: Not player's turn."); return; }
            tm.EndPlayerTurnButton();
        }
    }
}