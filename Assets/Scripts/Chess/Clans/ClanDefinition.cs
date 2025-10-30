using UnityEngine;

namespace Chess
{
    [CreateAssetMenu(menuName = "Clans/Clan Definition")]
    public class ClanDefinition : ScriptableObject
    {
        public string clanName;
        public Color uiColor = Color.white;

        [Header("Leader")]
        public Queen queenPrefab;               // leader piece prefab for this clan

        [Header("Abilities (in order)")]
        public AbilitySO[] abilities;
    }
}