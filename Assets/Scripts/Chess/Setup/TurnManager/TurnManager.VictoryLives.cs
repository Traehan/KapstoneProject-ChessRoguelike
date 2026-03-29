using UnityEngine;

namespace Chess
{
    public partial class TurnManager
    {
        void UpdateLivesUI()
        {
            if (livesText != null)
                livesText.text = $"Lives: {playerLives}";
        }

        void LoseLifeAndDespawn(Piece enemy)
        {
            if (board == null || enemy == null) return;

            bool captured = board.CapturePiece(enemy);
            if (!captured) return;

            playerLives = Mathf.Max(0, playerLives - 1);
            UpdateLivesUI();

            if (playerLives <= 0)
            {
                GameEvents.OnEncounterLost?.Invoke();
                FindObjectOfType<GameOverUI>()?.ShowGameOver();
            }
        }

        int PlayerHomeRankY()
        {
            return PlayerTeam == Team.White ? 0 : board.rows - 1;
        }

        bool EnemyReachedPlayerSide(Piece p)
        {
            return p.Coord.y == PlayerHomeRankY();
        }

        void ShowWinPanel()
        {
            FindObjectOfType<GameWinUI>()?.ShowWin();
        }

        void PlayerWon()
        {
            OnPlayerWon?.Invoke();
            GameEvents.OnEncounterWon?.Invoke();
            ShowWinPanel();
            SetPhase(TurnPhase.Cleanup);
        }
    }
}