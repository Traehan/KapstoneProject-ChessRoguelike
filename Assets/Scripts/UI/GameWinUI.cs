using UnityEngine;
using TMPro;

namespace Chess
{
    public class GameWinUI : MonoBehaviour
    {
        [SerializeField] GameObject winPanel;
        [SerializeField] TextMeshProUGUI winText;

        void Awake()
        {
            if (winPanel != null) winPanel.SetActive(false);
        }

        public void ShowWin()
        {
            if (winPanel != null) winPanel.SetActive(true);
            if (winText != null)  winText.text = "YOU WIN!";
        }
    }
}