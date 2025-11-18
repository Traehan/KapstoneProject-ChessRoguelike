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
        // =========================
        //  Singleton & Public State
        // =========================
        public static TurnManager Instance { get; private set; }
        public Team PlayerTeam => playerTeam;

        public TurnPhase Phase { get; private set; } = TurnPhase.PlayerTurn;
        public int CurrentAP { get; private set; }

        public bool IsPlayerTurn  => Phase == TurnPhase.PlayerTurn;
        public bool CanPlayerAct  => IsPlayerTurn && CurrentAP > 0;

        public HashSet<Piece> MovedThisPlayerTurnSnapshot => _movedThisPlayerTurn;

        // Events / Signals
        public System.Action<int, int> OnAPChanged;     // (current, max)
        public System.Action<TurnPhase> OnPhaseChanged;
        public event System.Action OnPlayerDefeated;
        public event System.Action OnPlayerWon;

        // =========================
        //  Inspector
        // =========================
        [Header("Refs")]
        [SerializeField] private ChessBoard board;
        [SerializeField] private Team playerTeam = Team.White;
        [SerializeField] private Team enemyTeam  = Team.Black;
        [SerializeField] private EncounterRunner encounterRunner;

        [Header("Turn Pacing")]
        [SerializeField, Range(0f, 2f)] float enemyMoveDelay     = 0.35f;
        [SerializeField, Range(0f, 2f)] float postEnemyTurnPause = 0.15f;

        [Header("Turn Config")]
        [Min(0)] public int apPerTurn = 3;

        // Enemy Intent Highlight
        readonly List<Vector2Int> _enemyIntents = new();
        readonly List<Vector2Int> _intentBuf    = new();
        readonly List<Vector2Int> _bossIntents  = new();   // boss-only intents
        [SerializeField] Color enemyIntentColor = new Color(1f, 0f, 0f, 0.85f);
        [SerializeField] Color bossIntentColor  = new Color(0.7f, 0.3f, 1f, 0.9f); // purpleRe

        // Player Lives
        [SerializeField] int playerLives = 3;
        [SerializeField] TextMeshProUGUI livesText;

        // --- Clan System ---
        [Header("Clan System")]
        [SerializeField] private ClanDefinition selectedClan;   // assign CLAN_IronMarch etc.
        [SerializeField] private Queen          queenLeader;    // runtime instance in scene

        ClanRuntime  _clan;
        AbilitySO[]  _abilities;
        IronMarch_QueenAura _ironMarchAura;                     // cached ref for pre-damage buff

        // Per-turn bookkeeping
        readonly HashSet<Piece> _movedThisPlayerTurn = new();
        bool _queenMovedThisTurn = false; // legacy/compat flag; abilities also track

        // --- Undo stack ---
        struct LastPlayerMove
        {
            public Piece mover;
            public Vector2Int from;
            public Vector2Int to;
            public int apSpent;

            // mover snapshots
            public int  moverHP_Before;
            public bool moverPawn_HadMovedBefore;

            // action shape
            public bool moverActuallyMoved;

            // defender snapshots
            public Piece defender;
            public Vector2Int defenderAt;
            public int  defenderHP_Before;
            public bool defenderDied;

            // Fortify / bookkeeping
            public int  moverFortify_Before;
            public bool moverWasMarkedMoved_Before;
            public bool queenMovedThisTurn_Before;
        }
        readonly List<LastPlayerMove> _moveStack = new();

        // =========================
        //  PUBLIC API (keep names)
        // =========================

        public void BeginEncounterFromPreparation()
        {
            if (Phase != TurnPhase.Preparation) return;
            BeginPlayerTurn(); // private
        }

        public void EndPlayerTurnButton()
        {
            if (!IsPlayerTurn) return;

            // clan abilities
            NotifyAbilitiesEndPlayerTurn();

            // NEW: piece runtimes
            NotifyAllPlayerPieceRuntimes_EndTurn();

            PaintAbilityHints(); // keep hints visible if any are static between turns
            StartCoroutine(EnemyTurnRoutine()); // private
        }

        public bool TrySpendAP(int amount = 1)
        {
            if (!IsPlayerTurn || CurrentAP < amount) return false;
            CurrentAP -= amount;
            OnAPChanged?.Invoke(CurrentAP, apPerTurn);
            return true;
        }

        /// <summary> Player-issued move (called by BoardInput). Handles AP and combat. </summary>
        public bool TryPlayerAct_Move(Piece piece, Vector2Int dest)
        {
            if (!ValidatePlayerMove(piece, dest)) return false;

            // Snapshot mover for undo
            var from                 = piece.Coord;
            int moverHP_before       = piece.currentHP;
            bool moverPawn_hadMoved  = (piece is Pawn pw) && pw.hasMoved;

            // Empty tile -> relocate
            if (!board.TryGetPiece(dest, out var target))
            {
                return HandlePlayerMoveToEmpty(piece, from, dest, moverHP_before, moverPawn_hadMoved);
            }

            // Ally at dest -> blocked
            if (target.Team == piece.Team) return false;

            // Enemy at dest -> pure attack (no relocation)
            return HandlePlayerAttack(piece, target, from, dest, moverHP_before, moverPawn_hadMoved);
        }

        public void RepaintEnemyIntentsOverlay()
        {
            if (board == null) return;

            if (_enemyIntents.Count > 0)
                board.Highlight(_enemyIntents, enemyIntentColor);

            if (_bossIntents.Count > 0)
                board.Highlight(_bossIntents, bossIntentColor);
        }


        public void RecomputeEnemyIntentsAndPaint()
        {
            if (board == null) return;

            board.ClearHighlights();
            ComputeEnemyIntents();             // private
            RepaintEnemyIntentsOverlay();

            // Victory check here so UI updates immediately after player actions
            EnsureEncounterRunnerBound();      // private
            if (encounterRunner != null && encounterRunner.IsVictoryReady(board))
            {
                PlayerWon();                   // private
                return;
            }
        }

        public bool TryUndoLastPlayerMove()
        {
            if (Phase != TurnPhase.PlayerTurn) return false;
            if (_moveStack.Count == 0 || board == null) return false;

            var m = _moveStack[_moveStack.Count - 1];

            if (!UndoRelocationIfNeeded(m)) { _moveStack.Clear(); return false; } // private

            // Restore HP
            m.mover.currentHP = m.moverHP_Before;

            // Restore defender if needed
            if (m.defender != null)
            {
                if (m.defenderDied)
                    board.RestoreCapturedPiece(m.defender, m.defenderAt);

                m.defender.currentHP = m.defenderHP_Before;
            }

            // Refund AP & pop
            CurrentAP = Mathf.Min(CurrentAP + m.apSpent, apPerTurn);
            OnAPChanged?.Invoke(CurrentAP, apPerTurn);
            _moveStack.RemoveAt(_moveStack.Count - 1);

            // Notify abilities about undo (clan)
            NotifyAbilitiesUndo(m); // private

            // NEW: notify runtimes about undo (mover & defender if present)
            m.mover?.GetComponent<PieceRuntime>()?.Notify_Undo();
            m.defender?.GetComponent<PieceRuntime>()?.Notify_Undo();

            // Refresh overlays/hints
            RecomputeEnemyIntentsAndPaint();
            PaintAbilityHints();
            return true;
        }

        public void RegisterLivesText(TextMeshProUGUI txt)
        {
            livesText = txt;
            UpdateLivesUI(); // private
        }

        // Kept for BoardInput calls; delegates to unified ability hint painter
        public void RepaintIronMarchHints()
        {
            PaintAbilityHints(); // private
        }

        // =========================
        //  UNITY LIFECYCLE (private)
        // =========================

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
            
            if (selectedClan == null && GameSession.I != null)
                selectedClan = GameSession.I.selectedClan;
            
            EnsureQueenLeaderBound();
            BuildClanRuntime();      // private
            BeginPreparation();      // private
        }

        // =========================
        //  TURN FLOW (private)
        // =========================

        void SetPhase(TurnPhase next)
        {
            Phase = next;
            OnPhaseChanged?.Invoke(Phase);
        }

        void BeginPreparation()
        {
            SetPhase(TurnPhase.Preparation);
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
            RecomputeEnemyIntentsAndPaint();

            // clan abilities
            NotifyAbilitiesBeginPlayerTurn(); // private

            // NEW: piece runtimes
            NotifyAllPlayerPieceRuntimes_BeginTurn();

            PaintAbilityHints();
        }

        IEnumerator EnemyTurnRoutine()
        {
            _moveStack.Clear();
            SetPhase(TurnPhase.EnemyTurn);

            // Acquire enemies in deterministic order
            var enemies = GetSortedEnemies(); // private

            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                if (!board.ContainsPiece(enemy)) continue;
                
                // NEW: Boss uses its own scripted turn (multi-ray attack + patrol move)
                if (enemy.TryGetComponent<BossEnemy>(out var boss))
                {
                    boss.ExecuteTurn(board);

                    if (enemyMoveDelay > 0f)
                        yield return new WaitForSeconds(enemyMoveDelay);

                    continue;
                }

                // 1) Determine destination/target (for normal enemies)
                if (!TryResolveEnemyTarget(enemy, out var target)) // private
                    continue;

                // 2) Execute move/attack
                TryMoveOrAttack(enemy, target); // private

                // 3) Pacing
                if (enemyMoveDelay > 0f) yield return new WaitForSeconds(enemyMoveDelay);
            }

            // Cleanup & pacing
            SetPhase(TurnPhase.Cleanup);
            if (postEnemyTurnPause > 0f) yield return new WaitForSeconds(postEnemyTurnPause);

            // Victory check (after enemies act)
            EnsureEncounterRunnerBound();
            if (encounterRunner != null && encounterRunner.IsVictoryReady(board))
            {
                PlayerWon();
                yield break;
            }

            if (board != null) board.ClearHighlights();
            BeginPlayerTurn();
        }

        // =========================
        //  MOVE / COMBAT (private)
        // =========================

        bool TryMoveOrAttack(Piece mover, Vector2Int to)
        {
            if (!board.InBounds(to)) return false;

            // Empty tile -> move
            if (!board.TryGetPiece(to, out var target))
            {
                bool moved = board.TryMovePiece(mover, to);

                if (!moved) return false;

                HandlePostMoveBookkeeping(mover);           // private
                HandleEnemyBackRankEscapeIfNeeded(mover);   // private
                return true;
            }

            // Ally blocks
            if (target.Team == mover.Team) return false;

            bool playerActing = (Phase == TurnPhase.PlayerTurn) && (mover.Team == playerTeam);
            bool enemyActing  = (Phase == TurnPhase.EnemyTurn) && (mover.Team == enemyTeam);

            if (playerActing)
            {
                ResolveCombat(mover, target, attackerIsPlayer: true,
                              out bool moverDied, out bool targetDied);

                if (moverDied) { board.RemovePiece(mover); return true; }
                if (targetDied) board.RemovePiece(target);
                return true;
            }

            if (enemyActing)
            {
                ResolveCombat(mover, target, attackerIsPlayer: false,
                              out bool moverDied, out bool targetDied);

                if (targetDied && !moverDied)
                {
                    board.RemovePiece(target);
                    board.TryMovePiece(mover, to);
                    HandleEnemyBackRankEscapeIfNeeded(mover);
                    return true;
                }

                if (moverDied && !targetDied) { board.RemovePiece(mover); return true; }

                if (moverDied && targetDied)
                {
                    board.RemovePiece(target);
                    board.RemovePiece(mover);
                    return true;
                }

                return true;
            }

            // Fallback simultaneous clash if phase/team mismatch ever occurs
            ResolveCombat(mover, target, attackerIsPlayer: false, out bool aDied, out bool bDied);
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
            // ----- Build base damage attacker -> defender -----
            int atkToDef_Base = Mathf.Max(0, attacker.attack);

            // Existing clan aura bonus (kept for compatibility)
            if (_ironMarchAura != null)
                atkToDef_Base += _ironMarchAura.GetAttackBonusIfEligible(_clan, attacker);

            // Piece runtime pre-damage modifiers (attacker & defender can both influence)
            var atkRuntime = attacker != null ? attacker.GetComponent<PieceRuntime>() : null;
            var defRuntime = defender != null ? defender.GetComponent<PieceRuntime>() : null;

            var atkCtx = new PieceAbilitySO.AttackCtx(attacker, defender, atkToDef_Base);
            atkRuntime?.CollectPreAttackModifiers(atkCtx);
            defRuntime?.CollectPreAttackModifiers(atkCtx);

            int atkToDef = Mathf.Max(0, atkCtx.baseDamage + atkCtx.damageDelta);

            // ----- Return fire (defender -> attacker) for enemy attacks -----
            int defToAtk = attackerIsPlayer ? 0 : Mathf.Max(0, defender.attack);
            // (You can later mirror a second AttackCtx for def->atk if you add defender-side keywords)

            // ----- Apply Fortify (respect bypass flag from abilities) -----
            int finalToDef = atkCtx.bypassFortify ? atkToDef : Mathf.Max(0, atkToDef - defender.fortifyStacks);
            int finalToAtk = Mathf.Max(0, defToAtk - attacker.fortifyStacks);

            // ----- Commit HP -----
            defender.currentHP -= finalToDef;
            attacker.currentHP -= finalToAtk;

            attackerDied = attacker.currentHP <= 0;
            defenderDied = defender.currentHP <= 0;

            // ----- Post-resolve callbacks -----
            atkRuntime?.Notify_AttackResolved(atkCtx);
            defRuntime?.Notify_AttackResolved(atkCtx);
        }

        // =========================
        //  ENEMY INTENTS (private)
        // =========================
        
        /// <summary>
        /// Special-case damage resolution for the Boss:
        /// - Defender takes damage as if the attacker were an enemy (no AP cost, etc.)
        /// - Attacker does NOT take any return damage (no retaliation).
        /// - Still supports abilities, fortify, and the Iron March aura.
        /// </summary>
        public void ResolveBossAttack(Piece attacker, Piece defender, out bool defenderDied)
        {
            defenderDied = false;
            if (attacker == null || defender == null) return;

            // Base damage attacker -> defender
            int atkToDef_Base = Mathf.Max(0, attacker.attack);

            // Existing clan aura bonus (if boss somehow uses it)
            if (_ironMarchAura != null)
                atkToDef_Base += _ironMarchAura.GetAttackBonusIfEligible(_clan, attacker);

            // Piece runtime pre-damage modifiers (same AttackCtx as normal)
            var atkRuntime = attacker.GetComponent<PieceRuntime>();
            var defRuntime = defender.GetComponent<PieceRuntime>();

            var atkCtx = new PieceAbilitySO.AttackCtx(attacker, defender, atkToDef_Base);
            atkRuntime?.CollectPreAttackModifiers(atkCtx);
            defRuntime?.CollectPreAttackModifiers(atkCtx);

            int atkToDef = Mathf.Max(0, atkCtx.baseDamage + atkCtx.damageDelta);

            // Apply Fortify on defender unless bypassed by abilities
            int finalToDef = atkCtx.bypassFortify
                ? atkToDef
                : Mathf.Max(0, atkToDef - defender.fortifyStacks);

            // Commit HP (boss takes ZERO damage here)
            defender.currentHP -= finalToDef;
            defenderDied = defender.currentHP <= 0;

            // Post-resolve callbacks for abilities
            atkRuntime?.Notify_AttackResolved(atkCtx);
            defRuntime?.Notify_AttackResolved(atkCtx);
        }


        void ComputeEnemyIntents()
        {
            _enemyIntents.Clear();
            _bossIntents.Clear();
            if (Phase != TurnPhase.PlayerTurn || board == null) return;

            foreach (var p in board.GetAllPieces())
            {
                if (p == null || p.Team != enemyTeam) continue;
                if (p is Pawn) continue; // your existing rule

                // Boss / special enemies with multi-tile intents
                if (p.TryGetComponent<IEnemyIntentProvider>(out var provider))
                {
                    _intentBuf.Clear();
                    provider.GetIntentTiles(board, _intentBuf);

                    foreach (var tile in _intentBuf)
                    {
                        if (board.InBounds(tile))
                            _bossIntents.Add(tile);
                    }

                    continue;
                }

                // Normal enemies: single destination tile
                if (!p.TryGetComponent<IEnemyBehavior>(out var beh)) continue;
                if (!beh.TryGetDesiredDestination(board, out var dest)) continue;
                if (!board.InBounds(dest)) continue;

                _enemyIntents.Add(dest);
            }
        }


        // =========================
        //  LIVES / VICTORY (private)
        // =========================

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
            SetPhase(TurnPhase.Cleanup);
        }

        // =========================
        //  ABILITIES / CLAN (private)
        // =========================

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

            // Sync the runtime context
            if (_clan != null) _clan.queen = queenLeader;

#if UNITY_EDITOR
            if (queenLeader == null)
                Debug.LogWarning("TurnManager: Could not find a player Queen in the scene. Assign queenLeader or spawn one.");
            else
                Debug.Log($"TurnManager: Bound queenLeader at {queenLeader.Coord}.");
#endif
        }

        void BuildClanRuntime()
        {
            if (selectedClan == null) return;

            _abilities = selectedClan.abilities ?? new AbilitySO[0];
            _clan      = new ClanRuntime(this, board, playerTeam, queenLeader, selectedClan);

            foreach (var a in _abilities) a?.OnClanEquipped(_clan);

            // Cache aura if present
            foreach (var a in _abilities)
                if (a is IronMarch_QueenAura aura) { _ironMarchAura = aura; break; }
        }

        void NotifyAbilitiesBeginPlayerTurn()
        {
            if (_abilities == null) return;
            foreach (var a in _abilities) a?.OnBeginPlayerTurn(_clan);
        }

        void NotifyAbilitiesEndPlayerTurn()
        {
            if (_abilities == null) return;
            foreach (var a in _abilities) a?.OnEndPlayerTurn(_clan);
        }

        void NotifyAbilitiesPieceMoved(Piece p)
        {
            if (_abilities == null) return;
            foreach (var a in _abilities) a?.OnPieceMoved(_clan, p);
        }

        void NotifyAbilitiesUndo(LastPlayerMove m)
        {
            if (_abilities == null) return;
            foreach (var a in _abilities) a?.OnUndo(_clan, m);
        }

        // =========================
        //  SMALL HELPERS (private)
        // =========================

        // Utility to enumerate all live player pieces safely
        IEnumerable<Piece> EnumeratePlayerPieces()
        {
            // Easiest robust approach: scan scene and keep those the board still tracks.
            foreach (var p in FindObjectsOfType<Piece>())
                if (p != null && p.Team == Team.White && board.ContainsPiece(p))
                    yield return p;
        }

        void NotifyAllPlayerPieceRuntimes_BeginTurn()
        {
            foreach (var p in EnumeratePlayerPieces())
                p.GetComponent<PieceRuntime>()?.Notify_BeginPlayerTurn();
        }

        void NotifyAllPlayerPieceRuntimes_EndTurn()
        {
            foreach (var p in EnumeratePlayerPieces())
                p.GetComponent<PieceRuntime>()?.Notify_EndPlayerTurn();
        }

        void EnsureEncounterRunnerBound()
        {
            if (encounterRunner == null)
                encounterRunner = FindObjectOfType<EncounterRunner>();
        }

        List<Piece> GetSortedEnemies()
        {
            var enemies = board.GetAllPieces()
                .Where(p => p.Team == enemyTeam)
                .ToList();

            enemies.Sort((a, b) =>
            {
                int keyA = a.Team == Team.White ? -a.Coord.y : a.Coord.y;
                int keyB = b.Team == Team.White ? -b.Coord.y : b.Coord.y;
                int cmp  = keyA.CompareTo(keyB);
                return (cmp != 0) ? cmp : a.Coord.x.CompareTo(b.Coord.x);
            });

            return enemies;
            // NOTE: if all enemies are Team.Black, the "Team.White" branches are harmless
        }

        bool TryResolveEnemyTarget(Piece enemy, out Vector2Int target)
        {
            // Honor Knight "locked intent" if present
            if (enemy is EnemyKnight ek && ek.HasLockedIntent)
            {
                target = ek.LockedIntent;
                ek.ClearLockedIntent();
                return true;
            }

            target = default;
            if (!enemy.TryGetComponent<IEnemyBehavior>(out var behavior))
                return false;

            if (!behavior.TryGetDesiredDestination(board, out var desired))
                return false;

            target = desired;
            return true;
        }

        bool ValidatePlayerMove(Piece piece, Vector2Int dest)
        {
            if (Phase != TurnPhase.PlayerTurn) return false;
            if (piece == null || board == null) return false;
            if (CurrentAP <= 0) return false;
            if (!board.InBounds(dest)) return false;
            return true;
        }

        bool HandlePlayerMoveToEmpty(Piece piece, Vector2Int from, Vector2Int dest,
                                     int moverHP_before, bool moverPawn_hadMoved)
        {
            int  fortBefore        = piece.fortifyStacks;
            bool wasMarkedMoved    = _movedThisPlayerTurn.Contains(piece);
            bool queenMovedBefore  = _queenMovedThisTurn;

            if (!board.TryMovePiece(piece, dest)) return false;

            // Bookkeeping
            _movedThisPlayerTurn.Add(piece);
            piece.ClearFortify();
            if (queenLeader != null && piece == queenLeader) _queenMovedThisTurn = true;

            // NEW: notify the mover's runtime
            piece.GetComponent<PieceRuntime>()?.Notify_PieceMoved(from, dest);

            // clan abilities
            NotifyAbilitiesPieceMoved(piece);
            PaintAbilityHints();

            SpendAP(1); // private

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

        bool HandlePlayerAttack(Piece attacker, Piece defender, Vector2Int from, Vector2Int dest,
                                int moverHP_before, bool moverPawn_hadMoved)
        {
            int defHP_before = defender.currentHP;

            ResolveCombat(attacker, defender, attackerIsPlayer: true,
                          out bool moverDied, out bool targetDied);

            if (moverDied)
            {
                board.RemovePiece(attacker);
                SpendAP(1);
                _moveStack.Clear(); // cannot undo death safely without fuller snapshots
                return true;
            }

            if (targetDied)
                board.CapturePiece(defender); // soft-capture for undo

            SpendAP(1);

            _moveStack.Add(new LastPlayerMove
            {
                mover = attacker,
                from = from,
                to = from,
                apSpent = 1,
                moverHP_Before = moverHP_before,
                moverPawn_HadMovedBefore = moverPawn_hadMoved,
                moverActuallyMoved = false,
                defender = defender,
                defenderAt = dest,
                defenderHP_Before = defHP_before,
                defenderDied = targetDied
            });

            RecomputeEnemyIntentsAndPaint();
            return true;
        }

        bool UndoRelocationIfNeeded(LastPlayerMove m)
        {
            if (m.moverActuallyMoved)
            {
                if (board.GetPieceAt(m.to) != m.mover) return false;

                board.ClearCell(m.to);
                board.PlaceWithoutCapture(m.mover, m.from);

                if (m.mover is Pawn pw)
                    pw.hasMoved = m.moverPawn_HadMovedBefore;

                // Restore Fortify and moved-set
                m.mover.fortifyStacks = m.moverFortify_Before;

                if (m.moverWasMarkedMoved_Before)
                    _movedThisPlayerTurn.Add(m.mover);
                else
                    _movedThisPlayerTurn.Remove(m.mover);

                _queenMovedThisTurn = m.queenMovedThisTurn_Before;
            }
            else
            {
                if (board.GetPieceAt(m.from) != m.mover) return false;
            }

            return true;
        }

        void SpendAP(int amount)
        {
            CurrentAP = Mathf.Max(0, CurrentAP - amount);
            OnAPChanged?.Invoke(CurrentAP, apPerTurn);
        }

        void HandlePostMoveBookkeeping(Piece mover)
        {
            if (Phase == TurnPhase.PlayerTurn && mover.Team == playerTeam)
            {
                _movedThisPlayerTurn.Add(mover);
                mover.ClearFortify();
                if (queenLeader != null && mover == queenLeader) _queenMovedThisTurn = true;

                // Note: EnemyTurn moves won't call piece runtime hooks here; only player moves do.
                NotifyAbilitiesPieceMoved(mover);
                PaintAbilityHints();
            }
        }

        void HandleEnemyBackRankEscapeIfNeeded(Piece mover)
        {
            if (Phase == TurnPhase.EnemyTurn && mover.Team == enemyTeam)
            {
                if (EnemyReachedPlayerSide(mover))
                    LoseLifeAndDespawn(mover);
            }
        }
    }
}
