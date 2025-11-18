// Assets/Scripts/Chess/Abilities/PieceLoadout.cs
using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    /// <summary>
    /// Author-time data placed on the piece prefab: innate abilities and default slot count.
    /// </summary>
    public class PieceLoadout : MonoBehaviour
    {
        [Min(1)] public int defaultUpgradeSlots = 2;

        [Header("Innate Abilities")]
        public List<PieceAbilitySO> innateAbilities = new();
    }
}