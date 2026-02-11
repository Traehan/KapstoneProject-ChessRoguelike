// using UnityEngine;
//
// namespace Chess
// {
//     public partial class TurnManager
//     {
//         void UpdateLivesUI()
//         {
//             if (livesText != null)
//                 livesText.text = $"Lives: {playerLives}";
//         }
//
//         void LoseLifeAndDespawn(Piece enemy)
//         {
//             board.RemovePiece(enemy);
//             playerLives--;
//             UpdateLivesUI();
//
//             if (playerLives <= 0)
//                 FindObjectOfType<GameOverUI>()?.ShowGameOver();
//         }
//
//         int PlayerHomeRankY()
//         {
//             return PlayerTeam == Team.White ? 0 : board.rows - 1;
//         }
//
//         bool EnemyReachedPlayerSide(Piece p)
//         {
//             return p.Coord.y == PlayerHomeRankY();
//         }
//
//         void ShowWinPanel()
//         {
//             FindObjectOfType<GameWinUI>()?.ShowWin();
//         }
//
//         void PlayerWon()
//         {
//             OnPlayerWon?.Invoke();
//             ShowWinPanel();
//             SetPhase(TurnPhase.Cleanup);
//         }
//     }
// }