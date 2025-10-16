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
        
    //use a custom icon prefab for THIS piece only
    public GameObject iconPrefabOverride;   // must contain DraggablePieceIcon
    public Color iconTint = Color.white;
    public Sprite frameSprite;
}