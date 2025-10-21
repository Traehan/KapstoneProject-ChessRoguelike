using UnityEngine;

namespace Chess
{
    public class RedoLastMoveProxy : MonoBehaviour
    {
        public void OnClickRedo()
        {
            var ok = TurnManager.Instance?.TryUndoLastPlayerMove() ?? false;
            if (!ok) Debug.Log("Redo failed or not available.");
        }
    }
}

