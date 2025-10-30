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

        [Header("Debug")]
        [SerializeField] bool runOnStart = true;
        
        [Header("PiecePlacer")]
        [SerializeField] PiecePlacer piecePlacer;

        int enemyTurnsStarted = 0;
        int roundsCompleted   = 0;
        
        //remember who we subscribed to
        TurnManager tm;
        bool subscribed;

        int _nextWaveIndex = 0;                     // if not already present
        public bool AllWavesStarted => encounter != null && _nextWaveIndex >= encounter.waves.Count;
        
        void Start()
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

            if (chosen == null)
            {
                Debug.LogError("EncounterRunner: No encounter found (field, SceneArgs, and GameSession are all null).");
                return;
            }

            // ✅ Make the runner’s field track the real encounter & reset wave/turn counters
            encounter          = chosen;               
            _nextWaveIndex     = 0;                    // reset between runs
            enemyTurnsStarted  = 0;                    // reset between runs
            roundsCompleted    = 0;                    // reset between runs

            StartCoroutine(RunEncounter(chosen));
            piecePlacer.PlaceClassicPawns();
        }

        
        //use later for when I create map and need to call when an encounter occurs
        public void StartWith(EncounterDefinition def)
        {
            if (def == null) { Debug.LogError("Encounter is null"); return; }
            StartCoroutine(RunEncounter(def));
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

                // SPAWN the wave (the same code you already have)
                foreach (var spec in wave.spawns)
                {
                    if (spec.piece == null || spec.piece.piecePrefab == null) continue;

                    var coord = spec.Resolve(board);
                    if (!board.InBounds(coord)) continue;

                    var p = board.PlacePiece(spec.piece.piecePrefab, coord, Team.Black);

                    var pieceComp = p as Piece;
                    if (pieceComp) pieceComp.EnsureDefinition(spec.piece);

                    if (!p.TryGetComponent<IEnemyBehavior>(out _))
                        p.gameObject.AddComponent<EnemySimpleChooser>();

                    if (wave.spawnInterval > 0f)
                        yield return new WaitForSeconds(wave.spawnInterval);
                }

                if (wave.postWavePause > 0f)
                    yield return new WaitForSeconds(wave.postWavePause);
            }
            TurnManager.Instance.RecomputeEnemyIntentsAndPaint();
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
