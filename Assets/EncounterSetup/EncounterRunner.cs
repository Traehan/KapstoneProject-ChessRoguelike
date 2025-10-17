using System.Collections;
using UnityEngine;

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
        
        void Start()
        {
            if (runOnStart && encounter != null)
                StartCoroutine(RunEncounter(encounter));
                piecePlacer.PlaceClassicPawns();
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
    }
}
