// Assets/Scripts/Chess/BoardSpawner.cs
using UnityEngine;

namespace Chess
{
    [DefaultExecutionOrder(-20)] // before TurnManager.Start()
    public class BoardSpawner : MonoBehaviour
    {
        [Header("Board")]
        public ChessBoard boardPrefab;   // <-- IMPORTANT: this prefab must have tilePrefab assigned in Inspector
        public Vector3 origin = Vector3.zero;
        public Transform parent;
        public int columns = 8;
        public int rows = 8;
        public float tileSize = 1f;

        ChessBoard _board;

        void Start()
        {
            // Spawn board exactly the way the original code expects
            _board = ChessBoard.Spawn(boardPrefab, origin, parent, columns, rows, tileSize);

            // Hand the TurnManager a board reference if it doesn't already have one
            var tm = FindObjectOfType<TurnManager>();
            if (tm != null)
            {
                var field = typeof(TurnManager).GetField("board",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (field != null && field.GetValue(tm) == null)
                    field.SetValue(tm, _board);
            }
        }
    }
}