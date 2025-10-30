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
        public Team PlayerTeam => playerTeam;
        public static TurnManager Instance { get; private set; }

        [Header("Refs")]
        [SerializeField] private ChessBoard board;
        [SerializeField] private Team playerTeam = Team.White; // player = White by default
        [SerializeField] private Team enemyTeam = Team.Black;

        [Header("Turn Pacing")]
        [SerializeField, Range(0f, 2f)] float enemyMoveDelay = 0.35f;
        [SerializeField, Range(0f, 2f)] float postEnemyTurnPause = 0.15f;

        [Header("Turn Config")]
        [Min(0)] public int apPerTurn = 3;

        // Enemy Intent Highlight
        readonly List<Vector2Int> _enemyIntents = new List<Vector2Int>();
        [SerializeField] Color enemyIntentColor = new Color(1f, 0f, 0f, 0.85f);

        // Player Lives
        [SerializeField] int playerLives = 3;
        [SerializeField] TextMeshProUGUI livesText;

        [SerializeField] EncounterRunner encounterRunner;

        // --- Clan System ---
        [Header("Clan System")]
        [SerializeField] private ClanDefinition selectedClan;   // assign CLAN_IronMarch etc.
        [SerializeField] private Queen queenLeader;             // runtime instance in scene
        public HashSet<Piece> MovedThisPlayerTurnSnapshot => _movedThisPlayerTurn;

        ClanRuntime _clan;
        AbilitySO[] _abilities;
        IronMarch_QueenAura _ironMarchAura;                    // cached ref for pre-damage buff

        // Per-turn bookkeeping
        readonly HashSet<Piece> _movedThisPlayerTurn = new HashSet<Piece>();
        bool _queenMovedThisTurn = false; // kept for legacy aura path if needed (abilities also track)

        public event System.Action OnPlayerDefeated;
        public event System.Action OnPlayerWon;

        // --- Undo stack ---
        struct LastPlayerMove
        {
            public Piece mover;
            public Vector2Int from;
            public Vector2Int to;
            public int apSpent;

            // mover snapshots
            public int moverHP_Before;
            public bool moverPawn_HadMovedBefore;

            // action shape
            public bool moverActuallyMoved;

            // defender snapshots
            public Piece defender;
            public Vector2Int defenderAt;
            public int defenderHP_Before;
            public bool defenderDied;

            // Iron March/armor snapshots
            public int moverFortify_Before;
            public bool moverWasMarkedMoved_Before;
            public bool queenMovedThisTurn_Before;
        }
        readonly List<LastPlayerMove> _moveStack = new List<LastPlayerMove>();

        public TurnPhase Phase { get; private set; } = TurnPhase.PlayerTurn;
        public int CurrentAP { get; private set; }

        public bool IsPlayerTurn => Phase == TurnPhase.PlayerTurn;
        public bool CanPlayerAct => IsPlayerTurn && CurrentAP > 0;

        public System.Action<int, int> OnAPChanged;    // (current, max)
        public System.Action<TurnPhase> OnPhaseChanged;

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
            EnsureQueenLeaderBound();

            // Build clan runtime
            if (selectedClan != null)
            {
                _abilities = selectedClan.abilities ?? new AbilitySO[0];
                _clan = new ClanRuntime(this, board, playerTeam, queenLeader, selectedClan);

                foreach (var a in _abilities) a?.OnClanEquipped(_clan);

                // cache aura if present
                foreach (var a in _abilities)
                    if (a is IronMarch_QueenAura aura) { _ironMarchAura = aura; break; }
            }

            BeginPreparation();
        }

        void SetPhase(TurnPhase next)
        {
            Phase = next;
            OnPhaseChanged?.Invoke(Phase);
        }

        void BeginPreparation()
        {
            Phase = TurnPhase.Preparation;
            OnPhaseChanged?.Invoke(Phase);
        }

        public void BeginEncounterFromPreparation()
        {
            if (Phase != TurnPhase.Preparation) return;
            BeginPlayerTurn();
        }

        void BeginPlayerTurn()
        {
            _moveStack.Clear();
            Phase = TurnPhase.PlayerTurn;
            CurrentAP = apPerTurn;
            OnPhaseChanged?.Invoke(Phase);
            OnAPChanged?.Invoke(CurrentAP, apPerTurn);

            _movedThisPlayerTurn.Clear();
            _queenMovedThisTurn = false;

            EnsureQueenLeaderBound();
            RecomputeEnemyIntentsAndPaint();

            if (_abilities != null)
            {
                foreach (var a in _abilities) a?.OnBeginPlayerTurn(_clan);
            }
            PaintAbilityHints(); // draw ability-based hints (e.g., queen aura)
        }

        public void EndPlayerTurnButton()
        {
            if (!IsPlayerTurn) return;

            if (_abilities != null)
            {
                foreach (var a in _abilities) a?.OnEndPlayerTurn(_clan);
            }
            PaintAbilityHints();

            StartCoroutine(EnemyTurnRoutine());
        }

        public bool TrySpendAP(int amount = 1)
        {
            if (!IsPlayerTurn || CurrentAP < amount) return false;
            CurrentAP -= amount;
            OnAPChanged?.Invoke(CurrentAP, apPerTurn);
            return true;
        }

        IEnumerator EnemyTurnRoutine()
        {
            _moveStack.Clear();
            Phase = TurnPhase.EnemyTurn;
            OnPhaseChanged?.Invoke(Phase);

            // get enemies
            List<Piece> enemies = board.GetAllPieces()
                .Where(p => p.Team == enemyTeam)
                .ToList();

            // sort
            enemies.Sort((a, b) =>
            {
                int keyA = a.Team == Team.White ? -a.Coord.y : a.Coord.y;
                int keyB = b.Team == Team.White ? -b.Coord.y : b.Coord.y;
                int cmp = keyA.CompareTo(keyB);
                return (cmp != 0) ? cmp : a.Coord.x.CompareTo(b.Coord.x);
            });

            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                if (!board.ContainsPiece(enemy)) continue;

                Vector2Int target;
                bool haveTarget = false;

                if (enemy is EnemyKnight ek && ek.HasLockedIntent)
                {
                    target = ek.LockedIntent;
                    haveTarget = true;
                    ek.ClearLockedIntent();
                }
                else
                {
                    if (!enemy.TryGetComponent<IEnemyBehavior>(out var behavior))
                        continue;
                    if (!behavior.TryGetDesiredDestination(board, out var desired))
                        continue;
                    target = desired;
                    haveTarget = true;
                }

                if (haveTarget)
                {
                    TryMoveOrAttack(enemy, target);
                }

                if (enemyMoveDelay > 0f)
                    yield return new WaitForSeconds(enemyMoveDelay);
            }

            // cleanup / victory
            Phase = TurnPhase.Cleanup;
            OnPhaseChanged?.Invoke(Phase);
            if (postEnemyTurnPause > 0f)
                yield return new WaitForSeconds(postEnemyTurnPause);

            if (encounterRunner == null) encounterRunner = FindObjectOfType<EncounterRunner>();
            if (encounterRunner != null && encounterRunner.IsVictoryReady(board))
            {
                PlayerWon();
                yield break;
            }

            if (board != null) board.ClearHighlights();
            BeginPlayerTurn();
        }

        /// <summary> Player-issued move (called by BoardInput). Handles AP and combat. </summary>
        public bool TryPlayerAct_Move(Piece piece, Vector2Int dest)
        {
            if (Phase != TurnPhase.PlayerTurn) return false;
            if (piece == null || board == null) return false;
            if (CurrentAP <= 0) return false;
            if (!board.InBounds(dest)) return false;

            // Snapshot mover state (for undo)
            int moverHP_before = piece.currentHP;
            bool moverPawn_hadMoved = (piece is Pawn pw) ? pw.hasMoved : false;
            var from = piece.Coord;

            // Empty tile -> relocate
            if (!board.TryGetPiece(dest, out var target))
            {
                int fortBefore = piece.fortifyStacks;
                bool wasMarkedMoved = _movedThisPlayerTurn.Contains(piece);
                bool queenMovedBefore = _queenMovedThisTurn;

                if (!board.TryMovePiece(piece, dest)) return false;

                // Bookkeep movement
                _movedThisPlayerTurn.Add(piece);
                piece.ClearFortify();
                if (queenLeader != null && piece == queenLeader) _queenMovedThisTurn = true;

                if (_abilities != null)
                    foreach (var a in _abilities) a?.OnPieceMoved(_clan, piece);
                PaintAbilityHints();

                // Spend AP
                CurrentAP -= 1;
                OnAPChanged?.Invoke(CurrentAP, apPerTurn);

                // Push undo
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

                    moverFortify_Before = fortBefore,
                    moverWasMarkedMoved_Before = wasMarkedMoved,
                    queenMovedThisTurn_Before = queenMovedBefore
                });

                RecomputeEnemyIntentsAndPaint();
                return true;
            }

            // Occupied by ally -> do nothing
            if (target.Team == piece.Team) return false;

            // --- PLAYER ATTACK (no move) ---
            int defHP_before = target.currentHP;

            ResolveCombat(attacker: piece, defender: target, attackerIsPlayer: true,
                out bool moverDied, out bool targetDied);

            if (moverDied)
            {
                board.RemovePiece(piece);
                CurrentAP -= 1; OnAPChanged?.Invoke(CurrentAP, apPerTurn);
                _moveStack.Clear();
                return true;
            }

            if (targetDied)
                board.CapturePiece(target);

            CurrentAP -= 1; OnAPChanged?.Invoke(CurrentAP, apPerTurn);

            _moveStack.Add(new LastPlayerMove
            {
                mover = piece,
                from = from,
                to = from,
                apSpent = 1,
                moverHP_Before = moverHP_before,
                moverPawn_HadMovedBefore = moverPawn_hadMoved,
                moverActuallyMoved = false,
                defender = target,
                defenderAt = dest,
                defenderHP_Before = defHP_before,
                defenderDied = targetDied
            });

            RecomputeEnemyIntentsAndPaint();
            return true;
        }

        /// <summary>
        /// Centralized move/attack resolution used by both player and enemies.
        /// Simultaneous damage if target occupied by opposite team.
        /// </summary>
        bool TryMoveOrAttack(Piece mover, Vector2Int to)
        {
            if (!board.InBounds(to)) return false;

            // Empty tile -> move
            if (!board.TryGetPiece(to, out var target))
            {
                bool moved = board.TryMovePiece(mover, to);

                if (moved && Phase == TurnPhase.PlayerTurn && mover.Team == playerTeam)
                {
                    _movedThisPlayerTurn.Add(mover);
                    mover.ClearFortify();
                    if (queenLeader != null && mover == queenLeader) _queenMovedThisTurn = true;

                    if (_abilities != null)
                        foreach (var a in _abilities) a?.OnPieceMoved(_clan, mover);
                    PaintAbilityHints();
                }

                // Enemy moved: check back-rank escape
                if (moved && Phase == TurnPhase.EnemyTurn && mover.Team == enemyTeam)
                {
                    if (EnemyReachedPlayerSide(mover))
                        LoseLifeAndDespawn(mover);
                }
                return moved;
            }

            // Ally blocks
            if (target.Team == mover.Team)
                return false;

            bool playerActing = (Phase == TurnPhase.PlayerTurn) && (mover.Team == playerTeam);
            bool enemyActing = (Phase == TurnPhase.EnemyTurn) && (mover.Team == enemyTeam);

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
                    board.RemovePiece(target);

                return true;
            }

            if (enemyActing)
            {
                ResolveCombat(attacker: mover, defender: target, attackerIsPlayer: false,
                    out bool moverDied, out bool targetDied);

                if (targetDied && !moverDied)
                {
                    board.RemovePiece(target);
                    board.TryMovePiece(mover, to);

                    if (EnemyReachedPlayerSide(mover))
                        LoseLifeAndDespawn(mover);

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

        // Damage rules:
        // - Player attacks: defender takes damage; attacker takes none.
        // - Enemy attacks: simultaneous exchange.
        void ResolveCombat(Piece attacker, Piece defender, bool attackerIsPlayer,
            out bool attackerDied, out bool defenderDied)
        {
            int atkToDef = Mathf.Max(0, attacker.attack);

            // Ask aura (if present) for conditional bonus pre-damage
            if (_ironMarchAura != null)
                atkToDef += _ironMarchAura.GetAttackBonusIfEligible(_clan, attacker);

            int defToAtk = attackerIsPlayer ? 0 : Mathf.Max(0, defender.attack);

            // Apply Fortify reductions
            int finalToDef = Mathf.Max(0, atkToDef - defender.fortifyStacks);
            int finalToAtk = Mathf.Max(0, defToAtk - attacker.fortifyStacks);

            defender.currentHP -= finalToDef;
            attacker.currentHP -= finalToAtk;

            attackerDied = attacker.currentHP <= 0;
            defenderDied = defender.currentHP <= 0;
        }

        void ComputeEnemyIntents()
        {
            _enemyIntents.Clear();
            if (Phase != TurnPhase.PlayerTurn || board == null) return;

            foreach (var p in board.GetAllPieces())
            {
                if (p == null) continue;
                if (p.Team != enemyTeam) continue;
                if (p is Pawn) continue;
                if (!p.TryGetComponent<IEnemyBehavior>(out var beh)) continue;

                if (beh.TryGetDesiredDestination(board, out var dest))
                    if (board.InBounds(dest))
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

            if (encounterRunner == null) encounterRunner = FindObjectOfType<EncounterRunner>();
            if (encounterRunner != null && encounterRunner.IsVictoryReady(board))
            {
                PlayerWon();
                return;
            }
        }

        // --- Undo ---
        public bool TryUndoLastPlayerMove()
        {
            if (Phase != TurnPhase.PlayerTurn) return false;
            if (_moveStack.Count == 0 || board == null) return false;

            var m = _moveStack[_moveStack.Count - 1];

            // 1) Undo relocation if it happened
            if (m.moverActuallyMoved)
            {
                if (board.GetPieceAt(m.to) != m.mover) { _moveStack.Clear(); return false; }

                board.ClearCell(m.to);
                board.PlaceWithoutCapture(m.mover, m.from);

                if (m.mover is Pawn pw)
                    pw.hasMoved = m.moverPawn_HadMovedBefore;

                // Restore armor state + moved set
                m.mover.fortifyStacks = m.moverFortify_Before;

                if (m.moverWasMarkedMoved_Before)
                    _movedThisPlayerTurn.Add(m.mover);
                else
                    _movedThisPlayerTurn.Remove(m.mover);

                _queenMovedThisTurn = m.queenMovedThisTurn_Before;
            }
            else
            {
                if (board.GetPieceAt(m.from) != m.mover) { _moveStack.Clear(); return false; }
            }

            // 2) Restore HP for mover
            m.mover.currentHP = m.moverHP_Before;

            // 3) Restore defender if any
            if (m.defender != null)
            {
                if (m.defenderDied)
                    board.RestoreCapturedPiece(m.defender, m.defenderAt);

                m.defender.currentHP = m.defenderHP_Before;
            }

            // 4) Refund AP and pop
            CurrentAP = Mathf.Min(CurrentAP + m.apSpent, apPerTurn);
            OnAPChanged?.Invoke(CurrentAP, apPerTurn);
            _moveStack.RemoveAt(_moveStack.Count - 1);

            // 5) Notify abilities (optional payload = the move record)
            if (_abilities != null)
                foreach (var a in _abilities) a?.OnUndo(_clan, m);

            // 6) Refresh overlays/hints
            RecomputeEnemyIntentsAndPaint();
            PaintAbilityHints();
            return true;
        }

        // --- Player lives / defeat ---
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

                var ui = FindObjectOfType<GameOverUI>();
                if (ui != null) ui.ShowGameOver();
            }
        }

        int PlayerHomeRankY()
        {
            return (PlayerTeam == Team.White) ? 0 : (board?.rows ?? 8) - 1;
        }

        bool EnemyReachedPlayerSide(Piece p)
        {
            if (p == null || p.Team != enemyTeam || board == null) return false;
            return p.Coord.y == PlayerHomeRankY();
        }

        public void RegisterLivesText(TextMeshProUGUI txt)
        {
            livesText = txt;
            UpdateLivesUI();
        }

        void ShowWinPanel()
        {
            var ui = FindObjectOfType<GameWinUI>();
            if (ui != null) ui.ShowWin();
        }

        void PlayerWon()
        {
            Debug.Log("Player won.");
            OnPlayerWon?.Invoke();
            ShowWinPanel();
            Phase = TurnPhase.Cleanup;
        }

        // --- Ability hint painting ---
        public void RepaintIronMarchHints()   // kept for your BoardInput calls; delegates to ability hints
        {
            PaintAbilityHints();
        }

        void PaintAbilityHints()
        {
            if (board == null || _abilities == null) return;
            foreach (var a in _abilities)
            {
                if (a != null && a.TryGetHintTiles(_clan, out var tiles, out var col))
                    board.Highlight(tiles, col);
            }
        }

        void EnsureQueenLeaderBound()
        {
            if (queenLeader != null && queenLeader.Board == board) return;

            queenLeader = board
                .GetAllPieces()
                .OfType<Queen>()
                .FirstOrDefault(q => q != null && q.Team == playerTeam);

            // NEW: sync the runtime context
            if (_clan != null)
                _clan.queen = queenLeader;

#if UNITY_EDITOR
            if (queenLeader == null)
                Debug.LogWarning("TurnManager: Could not find a player Queen in the scene. Assign queenLeader or spawn one.");
            else
                Debug.Log($"TurnManager: Bound queenLeader at {queenLeader.Coord}.");
#endif
        }
    }
}
