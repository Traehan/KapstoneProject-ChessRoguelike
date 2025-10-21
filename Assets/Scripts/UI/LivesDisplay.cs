using TMPro;
using UnityEngine;

namespace Chess
{
    public class LivesDisplay : MonoBehaviour
    {
        public TextMeshProUGUI text;

        void Awake()
        {
            // Find TurnManager and give it our text reference
            var tm = FindObjectOfType<TurnManager>();
            if (tm != null)
                tm.RegisterLivesText(text);
        }
    }
}