using UnityEngine;

namespace Chess
{
    // Ensure this runs before TurnManager.Start()
    [DefaultExecutionOrder(-10)]
    public class EncounterSetup : MonoBehaviour
    {
        [SerializeField] private ChessBoard board;

        [Header("Enemy Prefabs")]
        [SerializeField] private Pawn enemyPawnPrefabVariant;          // has EnemyPawnBehavior on it
        [SerializeField] private EnemyRook enemyRookPrefab;            // add in Inspector
        [SerializeField] private EnemyKnight enemyKnightPrefab;        // add in Inspector
        [SerializeField] private EnemyBishop enemyBishopPrefab;        // add in Inspector

        [Header("Player Helpers")]
        public PiecePlacer PiecePlacer;                                // your existing player pawn placer

        void Start()
        {
            // Clean slate, then spawn enemies + player pawns
            board.RemovePiecesOfTeam(Team.Black);
            board.Rebuild();

            SpawnEnemyElitesRow();  // NEW: rook/knight/bishop on top row
            SpawnEnemyWave();       // your existing row of enemy pawns (top-1 row)

            PiecePlacer.PlaceClassicPawns(); // your player pawns
        }

        // ---------- Enemy pawns (existing) ----------
        void SpawnEnemyPawn(Vector2Int coord)
        {
            var piece = board.PlacePiece(enemyPawnPrefabVariant, coord, Team.Black);
            var pawn  = piece as Pawn;

            if (pawn != null && !pawn.TryGetComponent<IEnemyBehavior>(out _))
                pawn.gameObject.AddComponent<EnemyPawnBehavior>(); // your existing pawn AI
        }

        public void SpawnEnemyWave()
        {
            int y = board.rows - 2; // top-1 row (Black moves -y)
            for (int x = 0; x < board.columns; x++)
                SpawnEnemyPawn(new Vector2Int(x, y));
        }

        // ---------- NEW: Enemy “elites” on the top row (y = rows - 1) ----------
        void SpawnEnemyElitesRow()
        {
            int yTop = board.rows - 1;

            // Choose three columns for R, N, B. We'll center them nicely.
            var trioXs = GetCenteredTrioXs(board.columns); // e.g., [2,3,4] on 7+ cols

            PlaceEnemy(enemyRookPrefab,   new Vector2Int(trioXs[0], yTop));
            PlaceEnemy(enemyKnightPrefab, new Vector2Int(trioXs[1], yTop));
            PlaceEnemy(enemyBishopPrefab, new Vector2Int(trioXs[2], yTop));
        }

        // Helper: center 3 slots across any board width >= 3
        int[] GetCenteredTrioXs(int cols)
        {
            if (cols < 3) return new[] { 0, Mathf.Min(1, cols - 1), Mathf.Min(2, cols - 1) };

            // center three consecutive columns
            if (cols % 2 == 1)
            {
                int mid = cols / 2;                     // odd width, perfect center
                return new[] { mid - 1, mid, mid + 1 };
            }
            else
            {
                int midRight = cols / 2;                // even width, bias slightly right
                return new[] { midRight - 2, midRight - 1, midRight };
            }
        }

        // Generic placer that ensures the piece is Team.Black and has an enemy behavior
        Piece PlaceEnemy(Piece prefab, Vector2Int coord)
        {
            if (prefab == null)
            {
                Debug.LogWarning($"EncounterSetup: Missing enemy prefab for {coord}.");
                return null;
            }

            var piece = board.PlacePiece(prefab, coord, Team.Black);

            // Make sure it has an AI behavior. Your Bishop/Rook/Knight legal-move logic lives
            // on EnemyBishop/EnemyRook/EnemyKnight; this adds the *chooser* if missing.
            if (piece != null && !piece.TryGetComponent<IEnemyBehavior>(out _))
                piece.gameObject.AddComponent<EnemySimpleChooser>();

            return piece;
        }
    }
}
