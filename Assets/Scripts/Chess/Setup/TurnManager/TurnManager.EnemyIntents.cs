using UnityEngine;

namespace Chess
{
    public partial class TurnManager
    {
        void ComputeEnemyIntents()
        {
            _enemyIntents.Clear();
            _bossIntents.Clear();
            if (Phase != TurnPhase.PlayerTurn) return;

            foreach (var p in board.GetAllPieces())
            {
                if (p.Team != enemyTeam || p is Pawn) continue;

                if (p.TryGetComponent<IEnemyIntentProvider>(out var provider))
                {
                    _intentBuf.Clear();
                    provider.GetIntentTiles(board, _intentBuf);
                    foreach (var t in _intentBuf)
                        if (board.InBounds(t)) _bossIntents.Add(t);
                    continue;
                }

                if (!p.TryGetComponent<IEnemyBehavior>(out var beh)) continue;
                if (beh.TryGetDesiredDestination(board, out var d))
                    _enemyIntents.Add(d);
            }
        }
    }
}