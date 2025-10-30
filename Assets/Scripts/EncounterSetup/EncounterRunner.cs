// Assets/Scripts/Encounters/EncounterRunner.cs
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Chess
{
    /// <summary>
    /// Orchestrates an EncounterDefinition: spawns waves based on triggers,
    /// tracks “enemy turns started” and “rounds completed”, and exposes
    /// AllWavesStarted + IsVictoryReady() used by TurnManager.
    /// </summary>
    [DefaultExecutionOrder(-5)]
    public class EncounterRunner : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private ChessBoard board;
        [SerializeField] private EncounterDefinition encounter;

        [Header("Debug")]
        [SerializeField] private bool runOnStart = true;

        [Header("Optional Helpers")]
        [SerializeField] private PiecePlacer piecePlacer;

        // === Publicly-observed state (names preserved) ===
        // TurnManager and UI expect these names/semantics
        int _nextWaveIndex = 0; // next wave to fire
        public bool AllWavesStarted => encounter != null && _nextWaveIndex >= encounter.waves.Count;

        // Counters for wave triggers
        int enemyTurnsStarted = 0; // how many times EnemyTurn began in this encounter
        int roundsCompleted   = 0; // a “round” ends when we return to PlayerTurn after at least one EnemyTurn

        // Subscriptions
        TurnManager tm;
        bool subscribed;

        // ========= Lifecycle =========
        void Awake()
        {
#if UNITY_2023_1_OR_NEWER
            if (board == null) board = Object.FindAnyObjectByType<ChessBoard>();
            if (tm == null)     tm    = Object.FindAnyObjectByType<TurnManager>();
#else
            if (board == null) board = FindObjectOfType<ChessBoard>();
            if (tm == null)     tm    = FindObjectOfType<TurnManager>();
#endif
        }

        void OnEnable()
        {
            TrySubscribe();
        }

        void OnDisable()
        {
            TryUnsubscribe();
        }

        void Start()
        {
            // Resolve encounter from (A) field, (B) SceneArgs.Payload, (C) GameSession.selectedEncounter
            var chosen = encounter;

            // B) scene payload
            var payload = GameManager.SceneArgs.Payload;
            if (chosen == null && payload is EncounterDefinition fromPayload)
            {
                chosen = fromPayload;
                GameManager.SceneArgs.Payload = null;
            }

            // C) global session pick
            if (chosen == null && GameSession.I != null && GameSession.I.selectedEncounter != null)
            {
                chosen = GameSession.I.selectedEncounter;
                GameSession.I.selectedEncounter = null;
            }

            if (!runOnStart || chosen == null) { encounter = chosen; return; }

            // Reset encounter-local counters/state
            encounter         = chosen;
            _nextWaveIndex    = 0;
            enemyTurnsStarted = 0;
            roundsCompleted   = 0;

            StartCoroutine(RunEncounter(encounter));

            // Optional: keep if you were seeding pawns for prototyping
            if (piecePlacer != null) piecePlacer.PlaceClassicPawns();
        }

        // ========= Public API (names preserved) =========

        /// <summary>Begin running an encounter definition now (replaces any current run).</summary>
        public void StartWith(EncounterDefinition def)
        {
            if (def == null) { Debug.LogError("EncounterRunner.StartWith: Encounter is null"); return; }
            StopAllCoroutines();
            encounter         = def;
            _nextWaveIndex    = 0;
            enemyTurnsStarted = 0;
            roundsCompleted   = 0;
            StartCoroutine(RunEncounter(def));
        }

        /// <summary>
        /// Main routine that applies preconditions, then plays waves in order.
        /// </summary>
        public IEnumerator RunEncounter(EncounterDefinition def)
        {
            if (board == null) yield break;
            if (def == null)   yield break;

            // Preconditions
            if (def.clearExistingBlackPieces)
            {
                board.RemovePiecesOfTeam(Team.Black);
                board.Rebuild(); // if your board uses a cached mesh/tiles, etc.
                yield return null; // give Unity one frame
            }

            // Sequentially process waves
            for (int i = 0; i < def.waves.Count; i++)
            {
                var wave = def.waves[i];
                if (wave == null) continue;

                // 1) Wait for trigger
                yield return WaitForWaveTrigger(wave);

                // Mark “next wave index” as progressed BEFORE spawning (important for IsVictoryReady timing)
                _nextWaveIndex = i + 1;
                Debug.Log($"EncounterRunner: Wave {i} triggered. _nextWaveIndex={_nextWaveIndex}");

                // 2) Spawn
                yield return SpawnWave(wave);

                // 3) Post-wave pause (breather)
                if (wave.postWavePause > 0f)
                    yield return new WaitForSeconds(wave.postWavePause);
            }

            // Done scheduling waves; victory will be detected by TurnManager
            Debug.Log("EncounterRunner: All waves started.");
        }

        /// <summary>
        /// Used by TurnManager to decide if win condition can show: all waves were scheduled AND no enemies remain.
        /// </summary>
        public bool IsVictoryReady(ChessBoard b)
        {
            if (b == null) return false;
            bool ready = AllWavesStarted && !BoardHasTeam(Team.Black);
            if (ready) Debug.Log("IsVictoryReady => TRUE (all waves started, board clear).");
            return ready;
        }

        // ========= Wave internals =========

        IEnumerator SpawnWave(EncounterWave wave)
        {
            if (board == null || wave == null || wave.spawns == null) yield break;

            foreach (var spec in wave.spawns)
            {
                if (spec.piece == null || spec.piece.piecePrefab == null) continue;

                var where = spec.Resolve(board);
                if (!board.InBounds(where)) continue;

                // Place on enemy team (Black)
                board.PlacePiece(spec.piece.piecePrefab, where, Team.Black);

                if (wave.spawnInterval > 0f)
                    yield return new WaitForSeconds(wave.spawnInterval);
            }
        }

        IEnumerator WaitForWaveTrigger(EncounterWave wave)
        {
            switch (wave.trigger)
            {
                case WaveTrigger.Immediate:
                    yield break;

                case WaveTrigger.AfterEnemyTurns:
                {
                    int anchor = enemyTurnsStarted;
                    while ((enemyTurnsStarted - anchor) < wave.amount)
                        yield return null;
                    yield break;
                }

                case WaveTrigger.AfterRounds:
                {
                    int anchor = roundsCompleted;
                    while ((roundsCompleted - anchor) < wave.amount)
                        yield return null;
                    yield break;
                }

                case WaveTrigger.AfterBoardCleared:
                {
                    while (BoardHasTeam(Team.Black))
                        yield return null;
                    yield break;
                }
            }
        }

        // ========= Counters / Turn subscriptions =========

        void TrySubscribe()
        {
            if (subscribed) return;
            if (tm == null)
            {
#if UNITY_2023_1_OR_NEWER
                tm = Object.FindAnyObjectByType<TurnManager>();
#else
                tm = FindObjectOfType<TurnManager>();
#endif
            }
            if (tm == null) return;

            tm.OnPhaseChanged += HandlePhaseChanged;
            subscribed = true;
        }

        void TryUnsubscribe()
        {
            if (!subscribed || tm == null) return;
            tm.OnPhaseChanged -= HandlePhaseChanged;
            subscribed = false;
        }

        void HandlePhaseChanged(TurnPhase phase)
        {
            // Count enemy turn starts
            if (phase == TurnPhase.EnemyTurn)
                enemyTurnsStarted++;

            // A “round” completes when we return to PlayerTurn after at least one enemy turn this cycle
            if (phase == TurnPhase.PlayerTurn && enemyTurnsStarted > 0)
                roundsCompleted++;
        }

        // ========= Board queries =========

        bool BoardHasTeam(Team team)
        {
            if (board == null) return false;
            // Safe and quick: early-out scan
            foreach (var p in board.GetAllPieces())
            {
                if (p != null && p.Team == team) return true;
            }
            return false;
        }
    }
}
