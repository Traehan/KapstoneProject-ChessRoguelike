using UnityEngine;
using ChessRL;

public class PiecePlacer : MonoBehaviour
{
    public ChessBoard board;      // assign in Inspector (your existing board in scene)
    public Pawn whitePawnPrefab;  // assign Pawn.prefab
    public Pawn blackPawnPrefab;  // assign Pawn.prefab (can be same prefab)
    

    [ContextMenu("Place Classic Pawns")]
    void PlaceClassicPawns()
    {
        if (board == null || whitePawnPrefab == null || blackPawnPrefab == null)
        { Debug.LogError("Assign board and pawn prefabs"); return; }

        // Classic ranks for 8x8; still works for any width >= 2
        int yWhite = 1;                 // 2nd rank (0-indexed)
        int yBlack = board.rows - 2;    // 7th rank on 8x8

        for (int x = 0; x < board.columns; x++)
        {
            board.PlacePiece(whitePawnPrefab, new Vector2Int(x, yWhite), Team.White);
            board.PlacePiece(blackPawnPrefab, new Vector2Int(x, yBlack), Team.Black);
        }
    }
    
    void Reset()  // called when component is added or Reset pressed
    {
        if (board == null) board = GetComponentInParent<ChessBoard>();
    }

    void OnValidate()
    {
#if UNITY_2023_1_OR_NEWER
        if (board == null) board = Object.FindAnyObjectByType<ChessBoard>();
#else
    if (board == null) board = FindObjectOfType<ChessBoard>();
#endif
    }

}