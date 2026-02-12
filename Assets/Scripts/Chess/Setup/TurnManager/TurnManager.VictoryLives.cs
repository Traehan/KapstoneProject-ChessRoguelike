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

            // Only lose a life if the board successfully captured it (prevents double-life loss)
            bool captured = board.CapturePiece(enemy);
            if (!captured) return;

            playerLives = Mathf.Max(0, playerLives - 1);
            UpdateLivesUI();

            if (playerLives <= 0)
                FindObjectOfType<GameOverUI>()?.ShowGameOver();
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
            ShowWinPanel();
            SetPhase(TurnPhase.Cleanup);
        }
    }
}