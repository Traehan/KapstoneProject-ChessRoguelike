using UnityEngine;
using TMPro;

namespace Chess
{
    public class GameOverUI : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] GameObject gameOverPanel;
        [SerializeField] TextMeshProUGUI gameOverText;

        void Awake()
        {
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);
        }

        public void ShowGameOver()
        {
            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);

            if (gameOverText != null)
                gameOverText.text = "GAME OVER";
        }
    }
}