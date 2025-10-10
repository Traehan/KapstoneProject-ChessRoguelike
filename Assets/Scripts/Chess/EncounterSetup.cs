using UnityEngine;

namespace Chess
{
    // Ensure this runs before TurnManager.Start()
    [DefaultExecutionOrder(-10)]
    public class EncounterSetup : MonoBehaviour
    {
        [SerializeField] private ChessBoard board;
        [Header("Prefabs")]
        [SerializeField] private Pawn enemyPawnPrefabVariant; // has EnemyPawnBehavior on it

        public PiecePlacer PiecePlacer;
        // If you don't use a variant, set this null and see the runtime add below.

        // Simple example formation: a row of pawns one row down from the top
        void Start()
        {
            board.RemovePiecesOfTeam(Team.Black);
            board.Rebuild();
            SpawnEnemyWave();
            PiecePlacer.PlaceClassicPawns();
        }

        void SpawnEnemyPawn(Vector2Int coord)
        {
            var piece = board.PlacePiece(enemyPawnPrefabVariant, coord, Team.Black);
            var pawn  = piece as Pawn;

            if (pawn != null && !pawn.TryGetComponent<IEnemyBehavior>(out _))
                pawn.gameObject.AddComponent<EnemyPawnBehavior>();
        }

        public void SpawnEnemyWave()
        {
            int y = board.rows - 2; // top-1 row (Black moves -y toward the player)
            for (int x = 0; x < board.columns; x++)
            {
                SpawnEnemyPawn(new Vector2Int(x, y));
            }
        }
    }
}