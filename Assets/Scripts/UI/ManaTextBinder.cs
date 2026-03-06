using TMPro;
using UnityEngine;

namespace Chess
{
    public class ManaTextBinder : MonoBehaviour
    {
        [SerializeField] TMP_Text label;

        void OnEnable()
        {
            var tm = TurnManager.Instance;
            if (tm == null) return;

            tm.OnManaChanged += HandleMana;
            HandleMana(tm.CurrentMana, tm.MaxMana);
        }

        void OnDisable()
        {
            var tm = TurnManager.Instance;
            if (tm == null) return;
            tm.OnManaChanged -= HandleMana;
        }

        void HandleMana(int current, int max)
        {
            if (label != null) label.text = $"{current}/{max}";
        }
    }
}