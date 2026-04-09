using UnityEngine;

namespace Chess
{
    [CreateAssetMenu(menuName =  "Chess/Piece Definition", fileName = "NewPiece")]
    public class PieceDefinition : ScriptableObject
    {
        [Header("Catalog")]
        public string displayName = "Pawn";
        public Sprite icon;
        public GameObject iconPrefabOverride;
        public int count = 1;

        [Header("Audio")]
        public SoundProfileSO soundProfile;

        [Header("VFX")]
        public GameObject attackImpactPrefab;

        public string Description;

        [Header("Spawn")]
        public Piece piecePrefab;

        [Header("Stats")]
        public int maxHP = 1;
        public int attack = 1;

        [Header("Movement Profile")]
        public bool forwardOnly = false;
        public bool passThroughFriendlies = false;
        [Min(1)] public int maxStride = 8;
        
        [Header("Shop")]
        public int shopPrice = 100;
    }
}