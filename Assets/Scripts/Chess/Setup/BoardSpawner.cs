// Assets/Scripts/Chess/BoardSpawner.cs
using UnityEngine;
using Chess;  // <-- add this to see Pawn, Team, Piece

namespace Chess
{
    [DefaultExecutionOrder(-20)] // make sure this runs before TurnManager.Start()
    public class BoardSpawner : MonoBehaviour
    {
        [Header("Board")] public ChessBoard boardPrefab;
        public int columns = 8;
        public int rows = 8;
        public float tileSize = 1f;

        [Header("Prefabs")] public Pawn playerPawnPrefab; // your player pawn prefab
        public Pawn enemyPawnPrefab; // your enemy pawn prefab (can be the same as player)

        ChessBoard _board;

        void Start()
        {
            // Spawn the board
            _board = ChessBoard.Spawn(boardPrefab, Vector3.zero, null, columns, rows, tileSize);

            // // Spawn formations
            // SpawnPlayerRow(y: 1); // 2nd rank (0-indexed)
            // SpawnEnemyRow(y: _board.rows - 2); // 7th rank on 8x8

            // (Optional) If TurnManager has a null board reference, let it find this one.
            var tm = FindObjectOfType<TurnManager>();
            // If your TurnManager already auto-finds a board in Awake/Start, you can skip this.
        }

        // void SpawnPlayerRow(int y)
        // {
        //     for (int x = 0; x < _board.columns; x++)
        //     {
        //         _board.PlacePiece(playerPawnPrefab, new Vector2Int(x, y), Team.White);
        //     }
        // }

        // void SpawnEnemyRow(int y)
        // {
        //     for (int x = 0; x < _board.columns; x++)
        //     {
        //         var placed = _board.PlacePiece(enemyPawnPrefab, new Vector2Int(x, y), Team.Black) as Pawn;
        //
        //         // Ensure enemy AI is present. If you created a prefab variant with the behavior,
        //         // this will already be there and TryGetComponent will succeed.
        //         if (placed != null && !placed.TryGetComponent<IEnemyBehavior>(out _))
        //         {
        //             placed.gameObject.AddComponent<EnemyPawnBehavior>();
        //         }
        //     }
        // }
    }
}
