using UnityEngine;

namespace Chess
{
    public class PlayerTurnUI : MonoBehaviour
    {
        public void EndTurn()
        {
            if (TurnManager.Instance != null)
                TurnManager.Instance.EndPlayerTurnButton();
        }
    }
}