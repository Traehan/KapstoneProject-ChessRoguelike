using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Chess
{
    
    
    public enum TurnPhase
    {
        Preparation,
        PlayerTurn,
        EnemyTurn,
        Cleanup
    }

    public class TurnManager : MonoBehaviour
    {
        // TurnManager.cs
        public Team PlayerTeam => playerTeam;

        public static TurnManager Instance { get; private set; }

        [Header("Refs")] [SerializeField] private ChessBoard board;
        [SerializeField] private Team playerTeam = Team.White; // player = White by default
        [SerializeField] private Team enemyTeam = Team.Black;

        [Header("Turn Config")] [Min(0)] public int apPerTurn = 3;
        private int NumberOfTurns = 0;
        private int WaveCounter = 0;

        public TurnPhase Phase { get; private set; } = TurnPhase.PlayerTurn;
        public int CurrentAP { get; private set; }

        public bool IsPlayerTurn => Phase == TurnPhase.PlayerTurn;
        public bool CanPlayerAct => IsPlayerTurn && CurrentAP > 0;

        public System.Action<int, int> OnAPChanged; // (current, max)
        public System.Action<TurnPhase> OnPhaseChanged;

        public EncounterSetup ES;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        void Start()
        {
            BeginPlayerTurn();
        }

        void BeginPlayerTurn()
        {
            Phase = TurnPhase.PlayerTurn;
            CurrentAP = apPerTurn;
            OnPhaseChanged?.Invoke(Phase);
            OnAPChanged?.Invoke(CurrentAP, apPerTurn);
        }

        public void EndPlayerTurnButton()
        {
            if (!IsPlayerTurn) return;
            StartCoroutine(EnemyTurnRoutine());
        }

        public bool TrySpendAP(int amount = 1)
        {
            if (!IsPlayerTurn || CurrentAP < amount) return false;
            CurrentAP -= amount;
            OnAPChanged?.Invoke(CurrentAP, apPerTurn);
            if (CurrentAP <= 0)
            {
                // Auto-end available if you want, but you asked for a button.
                // StartCoroutine(EnemyTurnRoutine());
            }

            return true;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        IEnumerator EnemyTurnRoutine()
        {
            Phase = TurnPhase.EnemyTurn;
            NumberOfTurns++;
            
            OnPhaseChanged?.Invoke(Phase);

            if (CheckNumberOfWaves())
            {
                if (CheckNumberOfTurns())
                {
                    ES.SpawnEnemyWave();
                    NumberOfTurns = 0;
                    WaveCounter++;
                    Debug.Log("New Wave!");
                }
            }
            else
            {
                Debug.Log("Waves Over");
            }
            
            // Gather all enemy pieces; if ChessBoard doesn't expose an API, fall back to FindObjectsOfType.
            List<Piece> enemies = board.GetAllPieces()
                .Where(p => p.Team == enemyTeam)
                .ToList();

            // Sort: front line first (closest to player side), then left-to-right as tie-breaker.
            enemies.Sort((a, b) =>
            {
                // Generalized along their forward direction:
                int keyA = a.Team == Team.White ? -a.Coord.y : a.Coord.y;
                int keyB = b.Team == Team.White ? -b.Coord.y : b.Coord.y;
                int cmp = keyA.CompareTo(keyB);
                if (cmp != 0) return cmp;
                return a.Coord.x.CompareTo(b.Coord.x);
            });

            foreach (var enemy in enemies)
            {
                if (enemy == null) continue; // may have died
                if (!board.ContainsPiece(enemy)) continue;

                // Ask any attached enemy behavior for its desired destination
                if (!enemy.TryGetComponent<IEnemyBehavior>(out var behavior))
                    continue;

                if (!behavior.TryGetDesiredDestination(board, out var desired))
                    continue;

                // Try to move or attack
                TryMoveOrAttack(enemy, desired);

                // Small pacing pause so you can later hook animations
                yield return new WaitForSeconds(0.05f);
            }

            // Cleanup (status ticks/hazards could go here)
            Phase = TurnPhase.Cleanup;
            OnPhaseChanged?.Invoke(Phase);
            yield return null;

            // Back to player
            BeginPlayerTurn();
        }

        /// <summary>
        /// Player-issued move (called by BoardInput). Handles AP and combat.
        /// </summary>
        public bool TryPlayerAct_Move(Piece piece, Vector2Int to)
        {
            if (!IsPlayerTurn || piece.Team != playerTeam) return false;
            if (!TrySpendAP(1)) return false;

            return TryMoveOrAttack(piece, to);
        }

        /// <summary>
        /// Centralized move/attack resolution used by both player and enemies.
        /// Simultaneous damage if target occupied by opposite team.
        /// </summary>
        bool TryMoveOrAttack(Piece mover, Vector2Int to)
        {
            if (!board.InBounds(to)) return false;

            // Empty tile -> normal move
            if (!board.TryGetPiece(to, out var target))
                return board.TryMovePiece(mover, to);

            // Ally blocks; do nothing
            if (target.Team == mover.Team)
                return false;

            // Determine who is acting under which phase
            bool playerActing = (Phase == TurnPhase.PlayerTurn) && (mover.Team == playerTeam);
            bool enemyActing = (Phase == TurnPhase.EnemyTurn) && (mover.Team == enemyTeam);

            // --- PLAYER ATTACKS (no move; defender only takes damage) ---
            if (playerActing)
            {
                ResolveCombat(attacker: mover, defender: target, attackerIsPlayer: true,
                    out bool moverDied, out bool targetDied);

                // Player shouldn't die when attacking on player's turn by design,
                // but we still guard against <= 0 HP in case stats change.
                if (moverDied)
                {
                    board.RemovePiece(mover);
                    // Action consumed even if something unexpected happened
                    return true;
                }

                if (targetDied)
                    board.RemovePiece(target); // player stays on original square

                // Action consumed (AP should decrement).
                return true;
            }

            // --- ENEMY ATTACKS (simultaneous; may move into tile if it kills) ---
            if (enemyActing)
            {
                ResolveCombat(attacker: mover, defender: target, attackerIsPlayer: false,
                    out bool moverDied, out bool targetDied);

                if (targetDied && !moverDied)
                {
                    // Enemy killed player -> take the square
                    board.RemovePiece(target);
                    board.TryMovePiece(mover, to);
                    return true;
                }

                if (moverDied && !targetDied)
                {
                    // Enemy died -> remove only enemy; stay otherwise
                    board.RemovePiece(mover);
                    return true;
                }

                if (moverDied && targetDied)
                {
                    // Both died -> remove both; tile ends up empty
                    board.RemovePiece(target);
                    board.RemovePiece(mover);
                    return true;
                }

                // Both survived -> both remain where they started
                return true;
            }

            // Fallback: if somehow turns/teams don't line up, do a safe simultaneous clash without movement.
            ResolveCombat(attacker: mover, defender: target, attackerIsPlayer: false,
                out bool aDied, out bool bDied);
            if (bDied) board.RemovePiece(target);
            if (aDied) board.RemovePiece(mover);
            return true; // action consumed
        }

// AttackerIsPlayer -> defender takes damage; attacker takes none when true.
// When false -> simultaneous exchange.
        void ResolveCombat(Piece attacker, Piece defender, bool attackerIsPlayer,
            out bool attackerDied, out bool defenderDied)
        {
            int damageToDef = Mathf.Max(0, attacker.attack);
            int damageToAtt = attackerIsPlayer ? 0 : Mathf.Max(0, defender.attack);

            defender.currentHP -= damageToDef;
            attacker.currentHP -= damageToAtt;

            attackerDied = attacker.currentHP <= 0;
            defenderDied = defender.currentHP <= 0;
        }

        private bool CheckNumberOfTurns()
        {
            if (NumberOfTurns == 3)
            {
                return true;
            }
                return false;
        }

        private bool CheckNumberOfWaves()
        {
            if (WaveCounter < 3)
            {
                return true;
            }
            return false;
        }
    }

}