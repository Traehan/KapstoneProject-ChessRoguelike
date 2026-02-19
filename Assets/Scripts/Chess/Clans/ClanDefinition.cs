using UnityEngine;

namespace Chess
{
    [CreateAssetMenu(menuName = "Clans/Clan Definition")]
    public class ClanDefinition : ScriptableObject
    {
        public string clanName;
        public string ClanPassiveDescription;
        public string QueenAuraDescription;
        public Sprite Queen;
        public Color uiColor = Color.white;

        [Header("Leader")]
        public Queen queenPrefab;               // leader piece prefab for this clan

        [Header("Queen Abilities (in order)")]
        public AbilitySO[] abilities;
        
        
    }
}