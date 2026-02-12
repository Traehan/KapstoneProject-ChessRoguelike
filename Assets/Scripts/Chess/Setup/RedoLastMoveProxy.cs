using UnityEngine;

namespace Chess
{
    public class RedoLastMoveProxy : MonoBehaviour
    {
        public void OnClickRedo()
        {
            var tm = TurnManager.Instance;
            if (tm == null)
            {
                Debug.LogWarning("Redo failed: TurnManager.Instance is null.");
                return;
            }

            tm.UndoButton(); // this is void
        }
    }
}