using UnityEngine;
using Card;

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
        public PieceDefinition queenDefinition;               // leader piece prefab for this clan

        [Header("Queen Abilities (in order)")]
        public AbilitySO[] abilities;
        
        public PieceDefinition[] StartingTroopPool;
        
        [Header("Starting Battle Deck")]
        [Tooltip("These are the actual cards added to the player's starting battle deck for this clan.")]
        public CardDefinitionSO[] startingBattleDeck;
        
        
    }
}