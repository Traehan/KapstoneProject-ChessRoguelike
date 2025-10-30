using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    
    public enum WaveTrigger
    {
        Immediate,          // spawn now (what you have now)
        AfterEnemyTurns,    // after N enemy turns have STARTED
        AfterRounds,        // after N full rounds (EnemyTurn -> PlayerTurn)
        AfterBoardCleared   // when there are no Team.Black pieces on the board
    }
    
    [CreateAssetMenu(menuName = "Chess/Encounters/Wave", fileName = "Wave_")]
    public class EncounterWave : ScriptableObject
    {
        [Tooltip("Pieces to spawn for this wave on enemy team (Black).")]
        public List<SpawnSpec> spawns = new List<SpawnSpec>();
        
        [Header("Trigger")]
        public WaveTrigger trigger = WaveTrigger.Immediate;
        [Min(0)] public int amount = 0;  // # of enemy turns or rounds to wait

        [Header("Pacing")]
        [Min(0f)] public float spawnInterval = 0.0f;    // delay between individual spawns
        [Min(0f)] public float postWavePause = 0.25f;   // breather after this wave completes
    }
}

