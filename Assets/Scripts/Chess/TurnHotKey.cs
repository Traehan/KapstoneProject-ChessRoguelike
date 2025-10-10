using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Chess
{
    public class TurnHotkeys : MonoBehaviour
    {
        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null &&
                Keyboard.current.eKey.wasPressedThisFrame)
#else
            if (Input.GetKeyDown(KeyCode.E))
#endif
            {
                var tm = TurnManager.Instance;
                if (tm != null && tm.IsPlayerTurn)
                    tm.EndPlayerTurnButton();
            }
        }
    }
}
