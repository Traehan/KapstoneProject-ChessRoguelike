// APTextBinder.cs
using UnityEngine;
using TMPro;

namespace Chess
{
    public class APTextBinder : MonoBehaviour
    {
        [SerializeField] TMP_Text label;              // optional; auto-fills
        [SerializeField] string format = "{0}/{1}";   // e.g., "3/3"
        [SerializeField] Color normalColor = Color.white;
        [SerializeField] Color zeroAPColor = Color.gray;

        TurnManager tm;
        int lastAP = int.MinValue, lastMax = int.MinValue;

        void Awake()
        {
            if (!label) label = GetComponent<TMP_Text>();
        }

        void Start()
        {
            TryBind();
            Refresh();
        }

        void Update()
        {
            if (tm == null) { TryBind(); return; }

            if (tm.CurrentAP != lastAP || tm.apPerTurn != lastMax)
                Refresh();
        }

        void TryBind()
        {
            tm = TurnManager.Instance ?? FindObjectOfType<TurnManager>();
        }

        void Refresh()
        {
            if (!label) return;
            lastAP = tm ? tm.CurrentAP : 0;
            lastMax = tm ? tm.apPerTurn : 0;

            label.text = string.Format(format, Mathf.Max(0, lastAP), Mathf.Max(0, lastMax));
            label.color = (lastAP > 0) ? normalColor : zeroAPColor;
        }
    }
}