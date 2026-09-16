using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    [CreateAssetMenu(menuName = "Chess/Encounters/Definition", fileName = "Encounter_")]
    public class EncounterDefinition : ScriptableObject
    {
        [Tooltip("Ordered waves for this encounter.")]
        public List<EncounterWave> waves = new List<EncounterWave>();

        [Header("Preconditions")]
        public bool clearExistingBlackPieces = true;  // e.g., for first encounter in a room

        [Header("Gauntlet Extension Point")]
        [Tooltip("Harder-Enemy Gauntlet challenge extension point: once elite enemy archetypes/waves exist, " +
                 "assign a wave here with 1-2 elite SpawnSpec entries. Left unassigned today - the Harder " +
                 "Enemy challenge falls back to cloning this encounter's own last authored wave's spawns.")]
        public EncounterWave eliteReinforcementWave;
    }
}

