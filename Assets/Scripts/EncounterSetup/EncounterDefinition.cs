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
    }
}

