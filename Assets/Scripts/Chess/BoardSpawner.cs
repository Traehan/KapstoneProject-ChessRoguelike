// Assets/Scripts/Chess/BoardSpawner.cs
using UnityEngine;

public class BoardSpawner : MonoBehaviour
{
    public ChessBoard boardPrefab;

    void Start()
    {
        // Example: standard chessboard at world origin
        var board = ChessBoard.Spawn(boardPrefab, Vector3.zero);

        // Example: another 10x10 board somewhere else
        // var alt = ChessBoard.Spawn(boardPrefab, new Vector3(12f, 0f, 0f), null, 10, 10, 1f);
    }
}

