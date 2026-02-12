using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    /// <summary>
    /// Special boss that:
    /// - Patrols along a fixed square path on the enemy side
    /// - Each enemy turn: first fires rook + bishop rays (multi-target, stays in place)
    /// - Then moves to the next patrol coordinate
    /// - Does NOT take damage when attacking (one-way damage).
    /// 
    /// NOTE: He still uses normal HP/attack values from PieceDefinition.
    /// </summary>
    [RequireComponent(typeof(Piece))]
    public class BossEnemy : MonoBehaviour, IEnemyBehavior, IEnemyIntentProvider
    {
        [Header("Patrol Path (Board Coords)")]
        [Tooltip("Boss will loop through these positions in order. " +
                 "Default: (2,1) -> (2,3) -> (5,3) -> (5,1) -> repeat.")]
        
        public int PatrolIndex
        {
            get => patrolIndex;
            set => patrolIndex = value;
        }

        public Vector2Int[] patrolPath = new Vector2Int[]
        {
            new Vector2Int(2, 1),
            new Vector2Int(2, 3),
            new Vector2Int(5, 3),
            new Vector2Int(5, 1)
        };

        [Tooltip("Index in patrolPath for the NEXT position the boss will move to.")]
        [SerializeField] private int patrolIndex = 0;

        private Piece self;

        // Rook directions (4)
        static readonly Vector2Int[] DIRS_Rook =
        {
            new Vector2Int(+1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, +1),
            new Vector2Int(0, -1),
        };

        // Bishop directions (4)
        static readonly Vector2Int[] DIRS_Bishop =
        {
            new Vector2Int(+1, +1),
            new Vector2Int(+1, -1),
            new Vector2Int(-1, +1),
            new Vector2Int(-1, -1),
        };

        void Awake()
        {
            self = GetComponent<Piece>();
        }

        /// <summary>
        /// Called from TurnManager during EnemyTurn for this boss instead of the generic AI.
        /// 1) Fire rook+bishop rays (multi-target, stays in place)
        /// 2) Move to next patrol coordinate.
        /// </summary>
        public void ExecuteTurn(ChessBoard board)
        {
            if (self == null || board == null) return;

            var tm = TurnManager.Instance;
            if (tm == null) return;

            // Step 1: ray attacks as commands (events fire inside the command)
            ExecuteMultiRayAsCommands(board, tm);

            // Step 2: patrol move as a command
            ExecutePatrolAsCommand(board);
        }


        // ---------- ATTACK LOGIC ----------
        
        void ExecuteMultiRayAsCommands(ChessBoard board, TurnManager tm)
        {
            // rook rays
            foreach (var dir in DIRS_Rook)
                TryRayAttackAsCommand(board, tm, dir);

            // bishop rays
            foreach (var dir in DIRS_Bishop)
                TryRayAttackAsCommand(board, tm, dir);
        }

        void TryRayAttackAsCommand(ChessBoard board, TurnManager tm, Vector2Int dir)
        {
            Vector2Int c = self.Coord;

            while (true)
            {
                c += dir;
                if (!board.InBounds(c)) break;

                if (!board.TryGetPiece(c, out var target))
                    continue; // empty

                // friendly blocks
                if (target.Team == self.Team)
                    break;

                // player piece -> command attack (one-way)
                var cmd = new BossRayAttackCommand(tm, board, self, c);
                if (cmd.Execute())
                    GameEvents.OnCommandExecuted?.Invoke(cmd);

                break; // stop after first piece
            }
        }

        void ExecutePatrolAsCommand(ChessBoard board)
        {
            if (patrolPath == null || patrolPath.Length == 0) return;

            // ensure valid index
            if (patrolIndex < 0 || patrolIndex >= patrolPath.Length)
                patrolIndex = FindClosestIndexToCurrent();

            int nextIndex = (patrolIndex + 1) % patrolPath.Length;
            Vector2Int nextCoord = patrolPath[nextIndex];

            var cmd = new BossPatrolCommand(board, self, this, nextIndex, nextCoord);
            if (cmd.Execute())
                GameEvents.OnCommandExecuted?.Invoke(cmd);
        }

        // ---------- PATROL / MOVEMENT ----------

        int FindClosestIndexToCurrent()
        {
            if (patrolPath == null || patrolPath.Length == 0) return 0;

            int bestIdx = 0;
            int bestDist = int.MaxValue;

            for (int i = 0; i < patrolPath.Length; i++)
            {
                var p = patrolPath[i];
                int dist = Mathf.Abs(p.x - self.Coord.x) + Mathf.Abs(p.y - self.Coord.y);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestIdx = i;
                }
            }

            return bestIdx;
        }

        // ---------- IEnemyBehavior (dummy) ----------

        /// <summary>
        /// We implement IEnemyBehavior so EnemyBehaviorFactory will NOT attach a different AI.
        /// TurnManager will not use this for the boss; it calls ExecuteTurn instead.
        /// </summary>
        public bool TryGetDesiredDestination(ChessBoard board, out Vector2Int dest)
        {
            dest = self != null ? self.Coord : default;
            return false; // "no normal destination"
        }

        // ---------- IEnemyIntentProvider ----------

        /// <summary>
        /// Provides only the tiles this boss would actually hit next enemy turn:
        /// the first non-friendly piece in each rook + bishop direction.
        /// No empty in-between tiles are highlighted.
        /// </summary>
        public void GetIntentTiles(ChessBoard board, List<Vector2Int> buffer)
        {
            buffer.Clear();
            if (self == null || board == null) return;

            foreach (var dir in DIRS_Rook)
                CollectRayTargetsOnly(board, dir, buffer);

            foreach (var dir in DIRS_Bishop)
                CollectRayTargetsOnly(board, dir, buffer);
        }

        /// <summary>
        /// Walk a ray; if we find a piece:
        /// - Friendly: blocks, no highlight.
        /// - Enemy (player): add that tile and stop.
        /// Empty tiles are ignored for highlighting.
        /// </summary>
        void CollectRayTargetsOnly(ChessBoard board, Vector2Int dir, List<Vector2Int> buffer)
        {
            Vector2Int c = self.Coord;

            while (true)
            {
                c += dir;
                if (!board.InBounds(c)) break;

                if (!board.TryGetPiece(c, out var piece))
                {
                    // empty: keep scanning
                    continue;
                }

                // Same-team piece: blocks the ray, but not a target
                if (piece.Team == self.Team)
                    break;

                // Opposing piece (player): this is where the boss would actually hit
                buffer.Add(c);
                break; // stop after first target in this direction
            }
        }

    }
}
