using UnityEngine;



namespace Chess
{
    [CreateAssetMenu(menuName =  "Chess/Piece Definition", fileName = "NewPiece")]
    public class PieceDefinition : ScriptableObject
    {
        [Header("Catalog")]
        public string displayName = "Pawn";
        public Sprite icon;
        public GameObject iconPrefabOverride; // already used by PrepPanel
        public int count = 1;

        [Header("Spawn")]
        public Piece piecePrefab;             // already used by PlacementManager

        [Header("Stats")]
        public int maxHP = 1;
        public int attack = 1;

        [Header("Movement Profile")]
        public bool forwardOnly = false;      // used by enemy pieces / pawns
        public bool passThroughFriendlies = false;
        [Min(1)] public int maxStride = 8;    // slide limit; 8=unlimited on 8x8

        // [Header("Abilities (future)")]
        // public Ability[] abilities;           // pluggable ScriptableObjects (see step 5)
    }
}