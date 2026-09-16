using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using GameManager;
using Unity.VisualScripting;


namespace Chess
{
    [DefaultExecutionOrder(-5)]
    public class EncounterRunner : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] ChessBoard board;
        [SerializeField] EncounterDefinition encounter;

        [Header("AI")]
        [Tooltip("If spawned piece has no enemy behavior, add this chooser automatically.")]
        [SerializeField] bool autoAddEnemyChooser = true;
        [SerializeField] DifficultyTier difficultyTier = DifficultyTier.Easy;

        [Header("Debug")]
        [Tooltip("If true, Start() resolves and immediately runs the encounter (legacy behavior). " +
                 "Set to false when a gating screen (e.g. GauntletScreen) is present in the scene - " +
                 "it will call StartWith(...) once the player confirms instead.")]
        [SerializeField] bool runOnStart = true;

        [Header("Gauntlet (Elite/Trial) Tuning")]
        [Tooltip("HP/Attack multiplier applied to every enemy spawned this encounter when the Stat Boost " +
                 "Gauntlet challenge was accepted.")]
        [SerializeField] float gauntletStatBoostMultiplier = 1.25f;
        public float GauntletStatBoostMultiplier => gauntletStatBoostMultiplier;

        [Header("PiecePlacer")]
        [SerializeField] PiecePlacer piecePlacer;

        int enemyTurnsStarted = 0;
        int roundsCompleted   = 0;

        // Set while a Gauntlet's Swarm/HarderEnemy bonus wave(s) are queued/spawning after the authored
        // waves finish, so IsVictoryReady() doesn't fire early just because the board is momentarily clear.
        bool gauntletBonusWavesPending = false;

        //remember who we subscribed to
        TurnManager tm;
        bool subscribed;

        int _nextWaveIndex = 0;                     // if not already present
        public bool AllWavesStarted => encounter != null && _nextWaveIndex >= encounter.waves.Count && !gauntletBonusWavesPending;

        /// <summary>The encounter resolved by Start() (field / SceneArgs.Payload / GameSession.selectedEncounter,
        /// in that priority order). Available even when runOnStart is false, so a gating screen (GauntletScreen)
        /// can read it for a preview without re-implementing the same resolution order.</summary>
        public EncounterDefinition PendingEncounter => encounter;

        void Start()
        {
            var chosen = ResolvePendingEncounter();

            if (chosen == null)
            {
                Debug.LogError("EncounterRunner: No encounter found (field, SceneArgs, and GameSession are all null).");
                return;
            }

            // Stash the resolved encounter back into the field either way, so PendingEncounter/StartWith
            // can find it later even when a gating screen is holding off the actual run.
            encounter = chosen;

            if (!runOnStart)
                return;

            BeginRun(chosen);
        }

        EncounterDefinition ResolvePendingEncounter()
        {
            var chosen = encounter;

            if (chosen == null && SceneArgs.Payload is EncounterDefinition fromPayload)
            {
                chosen = fromPayload;
                SceneArgs.Payload = null;
            }

            if (chosen == null && GameSession.I != null)
            {
                chosen = GameSession.I.selectedEncounter;
                GameSession.I.selectedEncounter = null;
            }

            return chosen;
        }

        void BeginRun(EncounterDefinition def)
        {
            encounter          = def;
            _nextWaveIndex     = 0;                    // reset between runs
            enemyTurnsStarted  = 0;                    // reset between runs
            roundsCompleted    = 0;                    // reset between runs
            gauntletBonusWavesPending = false;

            StartCoroutine(RunEncounter(def));
        }

        void Update()
        {
            if (!Application.isEditor && !Debug.isDebugBuild)
                return;

            if (Input.GetKeyDown(KeyCode.F2))
            {
                var turnManager = TurnManager.Instance ?? FindObjectOfType<TurnManager>();
                if (turnManager != null)
                    turnManager.DevForceEncounterWin();
            }
        }


        // Called by a gating screen (e.g. GauntletScreen) once the player confirms Start Encounter,
        // instead of letting Start() auto-run. Pass null to use whatever Start() already resolved.
        public void StartWith(EncounterDefinition def)
        {
            def = def != null ? def : encounter;

            if (def == null) { Debug.LogError("Encounter is null"); return; }
            BeginRun(def);
        }

        IEnumerator EnsureSubscribed()
        {
            // already good?
            if (subscribed && tm != null) yield break;

            // wait until a TurnManager exists
            while (tm == null)
            {
                tm = TurnManager.Instance ?? FindObjectOfType<TurnManager>();
                if (tm == null) { yield return null; continue; }
            }

            if (!subscribed)
            {
                tm.OnPhaseChanged += HandlePhaseChanged;
                subscribed = true;
            }
        }

        void OnEnable() { StartCoroutine(EnsureSubscribed()); }

        void OnDisable()
        {
            if (subscribed && tm != null)
                tm.OnPhaseChanged -= HandlePhaseChanged;
            subscribed = false;
            tm = null;
        }

        void HandlePhaseChanged(TurnPhase phase)
        {
            if (phase == TurnPhase.EnemyTurn)
                enemyTurnsStarted++;

            // define a “round” as EnemyTurn finishing and we’re back to PlayerTurn
            if (phase == TurnPhase.PlayerTurn && enemyTurnsStarted > 0)
                roundsCompleted++;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public IEnumerator RunEncounter(EncounterDefinition def)
        {

            if (def.clearExistingBlackPieces)
            {
                board.RemovePiecesOfTeam(Team.Black);
                board.Rebuild();
                yield return null;
            }

            foreach (var wave in def.waves)
            {

                // WAIT for the trigger
                yield return WaitForWaveTrigger(wave);

                _nextWaveIndex++;
                Debug.Log("_nextWaveIndexTriggered");

                // SPAWN the wave
                yield return SpawnWaveSpawns(wave);
            }

            // Gauntlet (Elite/Trial): if accepted, apply the relevant post-authored-wave modifier.
            // StatBoost/ManaHandicap/EnergyHandicap don't touch this coroutine (StatBoost is applied
            // inline in SpawnWaveSpawns; the handicaps are applied once in TurnManager.Awake()).
            var gs = GameSession.I;
            if (gs != null && gs.gauntletAccepted &&
                (gs.pendingGauntletChallenge == GauntletChallengeType.Swarm ||
                 gs.pendingGauntletChallenge == GauntletChallengeType.HarderEnemy))
            {
                gauntletBonusWavesPending = true;
                yield return ApplyGauntletPostEncounterModifiers(def, gs.pendingGauntletChallenge);
                gauntletBonusWavesPending = false;
            }

            TurnManager.Instance.RecomputeEnemyIntentsAndPaint();
        }

        IEnumerator ApplyGauntletPostEncounterModifiers(EncounterDefinition def, GauntletChallengeType challenge)
        {
            switch (challenge)
            {
                case GauntletChallengeType.Swarm:
                    // Two bonus waves, staggered a round apart - "one turn after the other, and then
                    // the turn after" - both clone the encounter's own last wave's spawns.
                    yield return SpawnBonusWave(def, overrideWave: null, waitRounds: 1);
                    yield return SpawnBonusWave(def, overrideWave: null, waitRounds: 1);
                    break;

                case GauntletChallengeType.HarderEnemy:
                    // One bonus wave, immediately after the authored waves finish (no swarm-style
                    // staggering). Uses the encounter's eliteReinforcementWave extension point once
                    // elite archetypes exist; falls back to cloning the last authored wave until then.
                    yield return SpawnBonusWave(def, def.eliteReinforcementWave, waitRounds: 1);
                    break;
            }
        }

        // Shared helper: waits waitRounds full rounds (same round-counting WaitForWaveTrigger's
        // AfterRounds case already uses), then spawns either overrideWave's spawns if provided, or a
        // clone of the encounter's own last authored wave's spawns otherwise.
        IEnumerator SpawnBonusWave(EncounterDefinition def, EncounterWave overrideWave, int waitRounds)
        {
            if (waitRounds > 0)
            {
                int anchor = roundsCompleted;
                while ((roundsCompleted - anchor) < waitRounds)
                    yield return null;
            }

            EncounterWave waveToSpawn = overrideWave;

            if (waveToSpawn == null && def != null && def.waves != null && def.waves.Count > 0)
                waveToSpawn = def.waves[def.waves.Count - 1];

            if (waveToSpawn == null)
                yield break;

            yield return SpawnWaveSpawns(waveToSpawn);
        }

        // Spawns every SpawnSpec in a wave (shared by the main authored-wave loop and SpawnBonusWave).
        // Applies the Gauntlet Stat Boost multiplier inline, right after each PlacePiece, when accepted.
        IEnumerator SpawnWaveSpawns(EncounterWave wave)
        {
            var gs = GameSession.I;
            bool applyStatBoost = gs != null && gs.gauntletAccepted &&
                                   gs.pendingGauntletChallenge == GauntletChallengeType.StatBoost;

            foreach (var spec in wave.spawns)
            {
                if (spec.piece == null || spec.piece.piecePrefab == null) continue;

                var coord = spec.Resolve(board);
                if (!board.InBounds(coord)) continue;

                var p = board.PlacePiece(spec.piece.piecePrefab, coord, Team.Black);

                var pieceComp = p as Piece;
                if (pieceComp) pieceComp.EnsureDefinition(spec.piece);

                if (applyStatBoost && pieceComp != null)
                {
                    pieceComp.maxHP = Mathf.Max(1, Mathf.RoundToInt(pieceComp.maxHP * gauntletStatBoostMultiplier));
                    pieceComp.currentHP = pieceComp.maxHP;
                    pieceComp.attack = Mathf.Max(1, Mathf.RoundToInt(pieceComp.attack * gauntletStatBoostMultiplier));
                }

                //attaches piece runtime
                var tm = TurnManager.Instance ?? FindObjectOfType<TurnManager>();
                var rt = p.GetComponent<PieceRuntime>();
                if (rt == null) rt = p.gameObject.AddComponent<PieceRuntime>();
                rt.Init(p, board, tm);

                GameEvents.OnPieceSpawned?.Invoke(p, coord);

                if (!p.TryGetComponent<IEnemyBehavior>(out _))
                    EnemyBehaviorFactory.EnsureBehaviorFor(p as Piece, difficultyTier);

                if (wave.spawnInterval > 0f)
                    yield return new WaitForSeconds(wave.spawnInterval);
            }

            if (wave.postWavePause > 0f)
                yield return new WaitForSeconds(wave.postWavePause);
        }

        IEnumerator WaitForWaveTrigger(EncounterWave wave)
        {
            switch (wave.trigger)
            {
                case WaveTrigger.Immediate:
                    yield break; // no wait

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

// small board query (add where convenient)
        bool BoardHasTeam(Team team)
        {
            // If ChessBoard already has a method, call it.
            // Otherwise, scan tiles/pieces list as you do elsewhere:
            for (int x = 0; x < board.columns; x++)
            for (int y = 0; y < board.rows; y++)
            {
                var p = board.GetPiece(new Vector2Int(x, y));
                if (p != null && p.Team == team) return true;
            }
            return false;
        }

        public bool IsVictoryReady(ChessBoard board)
        {
            if (board == null) return false;
            bool ready = AllWavesStarted && !BoardHasTeam(Team.Black);
            if (ready) Debug.Log("IsVictoryReady: TRUE (all waves started and board clear)");
            return ready;
        }

    }
}
