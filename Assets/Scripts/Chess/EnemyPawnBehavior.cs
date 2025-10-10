using UnityEngine;

namespace Chess
{
    [RequireComponent(typeof(Piece))]
    public class EnemyPawnBehavior : MonoBehaviour, IEnemyBehavior
    {
        private Piece _piece;

        void Awake() => _piece = GetComponent<Piece>();

        public bool TryGetDesiredDestination(ChessBoard board, out Vector2Int dest)
        {
            int dir = (int)_piece.Team; // +1 for White, -1 for Black
            var diagL = _piece.Coord + new Vector2Int(-1, dir);
            var diagR = _piece.Coord + new Vector2Int(+1, dir);

            // 1) Prefer capture if any enemy (player) piece is on a diagonal
            Piece bestTarget = null;
            Vector2Int bestDest = default;

            // left diag
            if (board.InBounds(diagL) && TryGetEnemyAt(board, diagL, out var leftTarget))
            {
                bestTarget = leftTarget;
                bestDest   = diagL;
            }

            // right diag — pick the lower-HP target; tie-break = keep current (left)
            if (board.InBounds(diagR) && TryGetEnemyAt(board, diagR, out var rightTarget))
            {
                if (bestTarget == null ||
                    rightTarget.currentHP < bestTarget.currentHP)
                {
                    bestTarget = rightTarget;
                    bestDest   = diagR;
                }
            }

            if (bestTarget != null)
            {
                dest = bestDest;     // TurnManager will resolve combat/move
                return true;
            }

            // 2) Otherwise, attempt to move straight forward 1
            dest = _piece.Coord + new Vector2Int(0, dir);
            if (!board.InBounds(dest)) return false; // off board -> no move
            return true;                              // TurnManager will handle blocking rules
        }

        private bool TryGetEnemyAt(ChessBoard board, Vector2Int c, out Piece enemy)
        {
            // Prefer TryGetPiece if you added it; fallback to GetPiece
            if (board.TryGetPiece(c, out var p))
            {
                if (p != null && p.Team != _piece.Team) { enemy = p; return true; }
            }
            else
            {
                p = board.GetPiece(c);
                if (p != null && p.Team != _piece.Team) { enemy = p; return true; }
            }

            enemy = null;
            return false;
        }
    }
}
