// Assets/Scripts/Prep/PieceDefinition.cs
using UnityEngine;
using Chess;

[CreateAssetMenu(menuName = "ChessRL/Piece Definition")]
public class PieceDefinition : ScriptableObject
{
    public string displayName;
    public Sprite icon;
    public Piece piecePrefab;   // later: Queen/Rook/Bishop/Knight prefabs
    [Min(0)] public int count = 1;
}