using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

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
        // ====== Public surface (unchanged) ======
        public Team PlayerTeam => playerTeam;
        public static TurnManager Instance { get; private set; }

        [Header("Refs")]
        [SerializeField] private ChessBoard board;
        [SerializeField] private Team playerTeam = Team.White; // player = White by default
        [SerializeField] private Team enemyTeam = Team.Black;

        [Header("Turn Pacing")]
        [SerializeField, Range(0f, 2f)] float enemyMoveDelay = 0.35f;     // per enemy action
        [SerializeField, Range(0f, 2f)] float postEnemyTurnPause = 0.15f; // after all enemies act

        [Header("Turn Config")]
        [Min(0)] public int apPerTurn = 3;

        // Enemy intent overlay
        readonly List<Vector2Int> _enemyIntents = new List<Vector2Int>();
        [SerializeField] Color enemyIntentColor = new Color(1f, 0f, 0f, 0.85f);

        // Player lives (UI)
        [SerializeField] int playerLives = 3;
        [SerializeField] TextMeshProUGUI livesText;

        [SerializeField] EncounterRunner encounterRunner;

        // ====== Clan / Iron March ======
        public enum ClanType { IronMarch /* future: BloodCourt, ObsidianVeil, ...*/ }

        [Header("Clan Setup")]
        [SerializeField] private ClanType selectedClan = ClanType.IronMarch;

        [Header("Iron March Hints")]
        [SerializeField] private Color ironMarchAuraColor = new Color(0.2f, 0.8f, 1f, 0.27f);

        // Assign in-scene player Queen
        [SerializeField] private Queen queenLeader;

        [SerializeField, Min(1)] private int ironMarchFortifyMax = 3;
        [SerializeField] private int ironMarchQueenAdjacencyBonus = 1;

        readonly HashSet<Piece> _movedThisPlayerTurn = new HashSet<Piece>();
        bool _queenMovedThisTurn = false;

        // Events (unchanged)
        public System.Action<int, int> OnAPChanged;  // (current, max)
        public System.Action<TurnPhase> OnPhaseChanged;
        public event System.Action OnPlayerDefeated;
        public event System.Action OnPlayerWon;

        // ====== Undo (unchanged names/shape) ======
        struct LastPlayerMove
        {
            public Piece mover;
            public Vector2Int from;
            public Vector2Int to;
            public int apSpent;

            public int moverHP_Before;
            public bool moverPawn_HadMovedBefore;
            public bool moverActuallyMoved;

            public Piece defender;
            public Vector2Int defenderAt;
            public int defenderHP_Before;
            public bool defenderDied;

            public int  moverFortify_Before;
            public bool moverWasMarkedMoved_Before;
            public bool queenMovedThisTurn_Before;
        }
        readonly List<LastPlayerMove> _moveStack = new List<LastPlayerMove>(); // stack

        public TurnPhase Phase { get; private set; } = TurnPhase.Preparation;
        public int CurrentAP { get; private set; }

        public bool IsPlayerTurn => Phase == TurnPhase.PlayerTurn;
        public bool CanPlayerAct => IsPlayerTurn && CurrentAP > 0;

        // ====== Bootstrap ======
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
            UpdateLivesUI();
            if (Phase == TurnPhase.PlayerTurn)
            {
                // Start of a run/session: ensure overlays are correct
                PaintAllOverlays();
            }
        }

        // ====== Simple public hooks (unchanged names) ======
        public void BeginEncounterFromPreparation()
        {
            if (Phase != TurnPhase.Preparation) return;
            BeginPlayerTurn();
        }

        public void EndPlayerTurnButton()
        {
            if (!IsPlayerTurn) return;
            GrantIronMarchFortifyIfSelected();
            StartCoroutine(EnemyTurnRoutine());
        }

        public bool TrySpendAP(int amount = 1)
        {
            if (!IsPlayerTurn || CurrentAP < amount) return false;
            CurrentAP -= amount;
            OnAPChanged?.Invoke(CurrentAP, apPerTurn);
            return true;
        }

        // ====== Core turn transitions (small helpers) ======
        void SetPhase(TurnPhase p)
        {
            Phase = p;
            OnPhaseChanged?.Invoke(Phase);
        }

        void BeginPlayerTurn()
        {
            _moveStack.Clear();
            SetPhase(TurnPhase.PlayerTurn);
            CurrentAP = apPerTurn;
            OnAPChanged?.Invoke(CurrentAP, apPerTurn);

            _movedThisPlayerTurn.Clear();
            _queenMovedThisTurn = false;

            EnsureQueenLeaderBound();
            PaintAllOverlays(); // enemy intents + Iron March (if applicable)
        }

        // ====== Player action (unchanged name/signature) ======
        /// <summary>Player-issued move or attack. Handles AP, undo snapshots, overlays, and victory checks.</summary>
        public bool TryPlayerAct_Move(Piece piece, Vector2Int dest)
        {
            if (Phase != TurnPhase.PlayerTurn) return false;
            if (piece == null || board == null) return false;
            if (CurrentAP <= 0) return false;
            if (!board.InBounds(dest)) return false;

            // Snapshot (for undo)
            int moverHP_before = piece.currentHP;
            bool moverPawn_hadMoved = (piece is Pawn pw) && pw.hasMoved;
            var from = piece.Coord;

            // Empty tile -> relocate
            if (!board.TryGetPiece(dest, out var target))
            {
                int  fortBefore       = piece.fortifyStacks;
                bool wasMarkedMoved   = _movedThisPlayerTurn.Contains(piece);
                bool queenMovedBefore = _queenMovedThisTurn;

                if (!board.TryMovePiece(piece, dest)) return false;

                // Mark per-turn flags
                _movedThisPlayerTurn.Add(piece);
                piece.ClearFortify();
                if (queenLeader != null && piece == queenLeader) _queenMovedThisTurn = true;

                // Spend AP (standardized)
                if (!TrySpendAP(1)) return false;

                // Push undo frame
                _moveStack.Add(new LastPlayerMove
                {
                    mover = piece,
                    from = from,
                    to = dest,
                    apSpent = 1,
                    moverHP_Before = moverHP_before,
                    moverPawn_HadMovedBefore = moverPawn_hadMoved,
                    moverActuallyMoved = true,
                    defender = null,
                    defenderHP_Before = 0,
                    defenderDied = false,
                    moverFortify_Before = fortBefore,
                    moverWasMarkedMoved_Before = wasMarkedMoved,
                    queenMovedThisTurn_Before = queenMovedBefore
                });

                // Victory check (player may have cleared last enemy)
                VictoryCheckDuringPlayer();

                // Overlays
                PaintAllOverlays();
                return true;
            }

            // Occupied by ally -> no-op
            if (target.Team == piece.Team) return false;

            // Attack only (no relocation)
            int defHP_before = target.currentHP;

            ResolveCombat(attacker: piece, defender: target, attackerIsPlayer: true,
                out bool moverDied, out bool targetDied);

            if (moverDied)
            {
                board.RemovePiece(piece);
                // Spend AP anyway; action consumed
                if (!TrySpendAP(1)) return true;
                _moveStack.Clear(); // safety: cannot reliably undo after removal
                PaintAllOverlays();
                return true;
            }

            if (targetDied)
                board.CapturePiece(target); // soft-capture for undo

            // Spend AP (standardized)
            if (!TrySpendAP(1)) return true;

            // Push undo frame (pure attack)
            _moveStack.Add(new LastPlayerMove
            {
                mover = piece,
                from = from,
                to = dest,
                apSpent = 1,
                moverHP_Before = moverHP_before,
                moverPawn_HadMovedBefore = moverPawn_hadMoved,
                moverActuallyMoved = false,
                defender = targetDied ? target : null,
                defenderAt = dest,
                defenderHP_Before = defHP_before,
                defenderDied = targetDied,
                moverFortify_Before = piece.fortifyStacks,
                moverWasMarkedMoved_Before = _movedThisPlayerTurn.Contains(piece),
                queenMovedThisTurn_Before = _queenMovedThisTurn
            });

            VictoryCheckDuringPlayer();
            PaintAllOverlays();
            return true;
        }

        // ====== Enemy turn ======
        IEnumerator EnemyTurnRoutine()
        {
            _moveStack.Clear();
            SetPhase(TurnPhase.EnemyTurn);

            // Snapshot enemy list
            List<Piece> enemies = board.GetAllPieces()
                .Where(p => p.Team == enemyTeam)
                .ToList();

            // Sort (frontline first along their forward)
            enemies.Sort((a, b) =>
            {
                int ay = a.Coord.y, by = b.Coord.y;
                // Enemy assumed moving "down" toward y==0 (player back rank); invert to act nearer to player first
                return ay.CompareTo(by); // lower y goes first
            });

            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                if (!board.ContainsPiece(enemy)) continue;

                // Decide destination
                bool haveTarget = false;
                Vector2Int target = enemy.Coord;

                // If enemy has a behavior, ask it; else skip
                if (enemy.TryGetComponent<IEnemyBehavior>(out var behavior))
                {
                    if (behavior.TryGetDesiredDestination(board, out var desired) && board.InBounds(desired))
                    {
                        target = desired;
                        haveTarget = true;
                    }
                }

                if (haveTarget)
                {
                    TryMoveOrAttack(enemy, target);
                }

                if (enemyMoveDelay > 0f)
                    yield return new WaitForSeconds(enemyMoveDelay);
            }

            // Enemy turn ended — victory check
            if (encounterRunner == null) encounterRunner = FindObjectOfType<EncounterRunner>();

            SetPhase(TurnPhase.Cleanup);
            if (postEnemyTurnPause > 0f)
                yield return new WaitForSeconds(postEnemyTurnPause);

            if (encounterRunner != null && encounterRunner.IsVictoryReady(board))
            {
                PlayerWon();
                yield break;
            }

            if (board != null) board.ClearHighlights();
            BeginPlayerTurn();
        }

        // ====== Shared move/attack resolution for enemies & misc ======
        bool TryMoveOrAttack(Piece mover, Vector2Int to)
        {
            if (!board.InBounds(to)) return false;

            // Empty
            if (!board.TryGetPiece(to, out var target))
            {
                bool moved = board.TryMovePiece(mover, to);

                // If an ENEMY just moved, check back-rank goal
                if (moved && Phase == TurnPhase.EnemyTurn && mover.Team == enemyTeam)
                {
                    if (EnemyReachedPlayerSide(mover))
                        LoseLifeAndDespawn(mover);
                }
                return moved;
            }

            // Ally block
            if (target.Team == mover.Team) return false;

            bool playerActing = (Phase == TurnPhase.PlayerTurn) && (mover.Team == playerTeam);
            bool enemyActing  = (Phase == TurnPhase.EnemyTurn) && (mover.Team == enemyTeam);

            // Player attacks (no move)
            if (playerActing)
            {
                ResolveCombat(attacker: mover, defender: target, attackerIsPlayer: true,
                    out bool moverDied, out bool targetDied);

                if (moverDied)
                {
                    board.RemovePiece(mover);
                    return true;
                }

                if (targetDied)
                {
                    board.RemovePiece(target);
                    return true;
                }

                return true;
            }

            // Enemy attacks (simultaneous)
            if (enemyActing)
            {
                ResolveCombat(attacker: mover, defender: target, attackerIsPlayer: false,
                    out bool moverDied, out bool targetDied);

                if (!moverDied && !targetDied)
                {
                    // Both survive: no move
                    return true;
                }

                if (!moverDied && targetDied)
                {
                    // Defender died -> occupy tile
                    board.RemovePiece(target);
                    board.TryMovePiece(mover, to);
                    return true;
                }

                if (moverDied && !targetDied)
                {
                    board.RemovePiece(mover);
                    return true;
                }

                if (moverDied && targetDied)
                {
                    board.RemovePiece(target);
                    board.RemovePiece(mover);
                    return true;
                }

                return true;
            }

            // Fallback: safe simultaneous clash
            ResolveCombat(attacker: mover, defender: target, attackerIsPlayer: false,
                out bool aDied, out bool bDied);
            if (bDied) board.RemovePiece(target);
            if (aDied) board.RemovePiece(mover);
            return true;
        }

        // ====== Combat resolution (unchanged logic) ======
        void ResolveCombat(Piece attacker, Piece defender, bool attackerIsPlayer,
            out bool attackerDied, out bool defenderDied)
        {
            // Basic schema:
            //  - Attacker deals its ATK reduced by defender fortify.
            //  - Defender retaliates (enemy turn only) similarly.
            //  - Player-turn attacks do not kill attacker by design, but we still compute safely.

            int atkToDef = Mathf.Max(0, attacker.attack - defender.fortifyStacks);
            int defToAtk = 0;

            // Enemy-turn retaliation (or symmetric if you enable for player too)
            if (!attackerIsPlayer)
                defToAtk = Mathf.Max(0, defender.attack - attacker.fortifyStacks);

            defender.currentHP -= atkToDef;
            attacker.currentHP -= defToAtk;

            attackerDied = attacker.currentHP <= 0;
            defenderDied = defender.currentHP <= 0;
        }

        // ====== Enemy intents (unchanged names) ======
        void ComputeEnemyIntents()
        {
            _enemyIntents.Clear();
            if (Phase != TurnPhase.PlayerTurn || board == null) return;

            foreach (var p in board.GetAllPieces())
            {
                if (p == null) continue;
                if (p.Team != enemyTeam) continue;
                if (p is Pawn) continue; // skip pawns for “significant” intents

                if (!p.TryGetComponent<IEnemyBehavior>(out var beh)) continue;
                if (!beh.TryGetDesiredDestination(board, out var dest)) continue;
                if (!board.InBounds(dest)) continue;

                _enemyIntents.Add(dest);
            }
        }

        public void RepaintEnemyIntentsOverlay()
        {
            if (board == null) return;
            if (_enemyIntents.Count == 0) return;
            board.Highlight(_enemyIntents, enemyIntentColor);
        }

        public void RecomputeEnemyIntentsAndPaint()
        {
            if (board == null) return;
            board.ClearHighlights();
            ComputeEnemyIntents();
            RepaintEnemyIntentsOverlay();
        }

        // ====== Undo (unchanged public name) ======
        public bool TryUndoLastPlayerMove()
        {
            if (Phase != TurnPhase.PlayerTurn) return false;
            if (_moveStack.Count == 0 || board == null) return false;

            var m = _moveStack[_moveStack.Count - 1];

            // 1) Restore mover’s position
            if (m.moverActuallyMoved)
            {
                // Expect mover currently at 'to'
                if (board.GetPieceAt(m.to) != m.mover) { _moveStack.Clear(); return false; }
                if (!board.TryMovePiece(m.mover, m.from)) { _moveStack.Clear(); return false; }
            }
            else
            {
                // Pure attack: mover should remain at 'from'
                if (board.GetPieceAt(m.from) != m.mover) { _moveStack.Clear(); return false; }
            }

            // 2) Restore mover HP & pawn “hasMoved”
            m.mover.currentHP = m.moverHP_Before;
            if (m.mover is Pawn pw) pw.hasMoved = m.moverPawn_HadMovedBefore;
            m.mover.fortifyStacks = m.moverFortify_Before;

            // 3) Restore defender if applicable
            if (m.defender != null)
            {
                if (m.defenderDied)
                    board.RestoreCapturedPiece(m.defender, m.defenderAt);
                m.defender.currentHP = m.defenderHP_Before;
            }

            // 4) Refund AP and pop stack
            CurrentAP = Mathf.Min(CurrentAP + m.apSpent, apPerTurn);
            OnAPChanged?.Invoke(CurrentAP, apPerTurn);
            _moveStack.RemoveAt(_moveStack.Count - 1);

            // 5) Restore per-turn flags
            if (m.moverWasMarkedMoved_Before)
                _movedThisPlayerTurn.Add(m.mover);
            else
                _movedThisPlayerTurn.Remove(m.mover);
            _queenMovedThisTurn = m.queenMovedThisTurn_Before;

            // 6) Refresh overlays
            PaintAllOverlays();
            return true;
        }

        // ====== Victory / defeat helpers ======
        void VictoryCheckDuringPlayer()
        {
            // If wave system says all waves started & no enemies, declare now.
            if (encounterRunner == null) encounterRunner = FindObjectOfType<EncounterRunner>();
            if (encounterRunner != null && encounterRunner.IsVictoryReady(board))
            {
                PlayerWon();
            }
        }

        void PlayerWon()
        {
            Debug.Log("Player won.");
            OnPlayerWon?.Invoke();
            ShowWinPanel();
            SetPhase(TurnPhase.Cleanup);
        }

        void ShowWinPanel()
        {
            var ui = FindObjectOfType<GameWinUI>();
            if (ui != null) ui.ShowWin();
        }

        // ====== Lives / UI ======
        void UpdateLivesUI()
        {
            if (livesText != null) livesText.text = $"Lives: {playerLives}";
        }

        void LoseLifeAndDespawn(Piece enemy)
        {
            if (enemy != null && board != null && board.ContainsPiece(enemy))
                board.RemovePiece(enemy);

            playerLives = Mathf.Max(0, playerLives - 1);
            UpdateLivesUI();

            if (playerLives <= 0)
            {
                Debug.Log("Player defeated.");
                OnPlayerDefeated?.Invoke();
                // You can trigger a GameOver panel here via another UI script.
            }
        }

        bool EnemyReachedPlayerSide(Piece mover)
        {
            if (board == null || mover == null) return false;
            // Player back rank assumed at y == 0
            return mover.Coord.y <= 0;
        }

        // ====== Iron March ======
        void GrantIronMarchFortifyIfSelected()
        {
            if (selectedClan != ClanType.IronMarch || board == null) return;

            // Any piece that did NOT move this turn gains up to max fortify.
            foreach (var p in board.GetAllPieces())
            {
                if (p == null || p.Team != playerTeam) continue;
                if (_movedThisPlayerTurn.Contains(p)) continue;
                p.AddFortify(ironMarchFortifyMax);
            }
        }

        public void RepaintIronMarchHints()
        {
            if (selectedClan != ClanType.IronMarch || board == null) return;

            EnsureQueenLeaderBound();
            if (queenLeader == null) return;
            if (_queenMovedThisTurn) return; // aura suppressed after queen moves

            // Adjacent-8 around queen
            var q = queenLeader.Coord;
            List<Vector2Int> adj = new List<Vector2Int>(8);
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                var c = new Vector2Int(q.x + dx, q.y + dy);
                if (board.InBounds(c)) adj.Add(c);
            }

            // Layered over existing highlights (no ClearHighlights here)
            board.Highlight(adj, ironMarchAuraColor);
        }

        void EnsureQueenLeaderBound()
        {
            if (queenLeader != null && queenLeader.Board == board) return;

            queenLeader = board
                .GetAllPieces()
                .OfType<Queen>()
                .FirstOrDefault(q => q != null && q.Team == playerTeam);

#if UNITY_EDITOR
            if (queenLeader == null)
                Debug.LogWarning("TurnManager: Could not find a player Queen in the scene. Assign queenLeader or spawn one.");
            else
                Debug.Log($"TurnManager: Bound queenLeader at {queenLeader.Coord}.");
#endif
        }

        // ====== Overlay bundler ======
        void PaintAllOverlays()
        {
            // Enemy intents first (clears), then Iron March aura on top
            RecomputeEnemyIntentsAndPaint();
            RepaintIronMarchHints();
        }

        // ====== Public small helpers you already rely on ======
        public void RegisterLivesText(TextMeshProUGUI txt)
        {
            livesText = txt;
            UpdateLivesUI();
        }
    }
}
