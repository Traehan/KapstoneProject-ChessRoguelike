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
        // TurnManager.cs
        public Team PlayerTeam => playerTeam;

        public static TurnManager Instance { get; private set; }

        [Header("Refs")] [SerializeField] private ChessBoard board;
        [SerializeField] private Team playerTeam = Team.White; // player = White by default
        [SerializeField] private Team enemyTeam = Team.Black;
        
        [Header("Turn Pacing")]
        [SerializeField, Range(0f, 2f)] float enemyMoveDelay = 0.35f;     // per enemy action
        [SerializeField, Range(0f, 2f)] float postEnemyTurnPause = 0.15f; // after all enemies act

        [Header("Turn Config")] [Min(0)] public int apPerTurn = 3;
        
        //Enemy Intent Highlight
        readonly List<Vector2Int> _enemyIntents = new List<Vector2Int>();
        [SerializeField] Color enemyIntentColor = new Color(1f, 0f, 0f, 0.85f); // red
        
        //Player Lives
        [SerializeField] int playerLives = 3;
        [SerializeField] TextMeshProUGUI livesText;
        
        [SerializeField] EncounterRunner encounterRunner;
        
        //FOR CLANS --> IRON MARCH
        // --- Clan / Leader (Iron March bootstrap) ---
        public enum ClanType { IronMarch /*, BloodCourt, ObsidianVeil, ...*/ }

        [Header("Clan Setup")]
        [SerializeField] private ClanType selectedClan = ClanType.IronMarch;
        [Header("Iron March Hints")]
        [SerializeField] private Color ironMarchAuraColor = new Color(1f, 0.85f, 0.2f, 1f); // soft gold

        // Assign your Iron Matriarch Queen prefab instance in-scene (player side)
        [SerializeField] private Queen queenLeader;

        // Iron March tuning knobs
        [SerializeField, Min(1)] private int ironMarchFortifyMax = 3;
        [SerializeField] private int ironMarchQueenAdjacencyBonus = 1;

        // Per-turn bookkeeping
        readonly HashSet<Piece> _movedThisPlayerTurn = new HashSet<Piece>();
        bool _queenMovedThisTurn = false;


        
        public event System.Action OnPlayerDefeated; // will use later to change to ExitBacktoMainMenu screen
        public event System.Action OnPlayerWon; //
        
        // --- Redo / Undo last player moves (stack) ---
        struct LastPlayerMove
        {
            public Piece mover; //The player piece
            public Vector2Int from; //original position
            public Vector2Int to; //position it moved to
            public int apSpent; //which is 1 AP

            // mover snapshots
            public int moverHP_Before; //saves the original hp before action
            public bool moverPawn_HadMovedBefore; //checks if it WAS the piece

            // action shape
            public bool moverActuallyMoved;   // true = a relocation occurred; false = pure attack

            // defender snapshots (only set when a defender existed)
            public Piece defender;
            public Vector2Int defenderAt;
            public int defenderHP_Before;
            public bool defenderDied;         // true if we removed defender
            
            public int  moverFortify_Before;           // fortify stacks of the mover before the action
            public bool moverWasMarkedMoved_Before;    // whether _movedThisPlayerTurn contained the mover
            public bool queenMovedThisTurn_Before;     // value of _queenMovedThisTurn before the action
        }
        readonly List<LastPlayerMove> _moveStack = new List<LastPlayerMove>(); // acts like a stack


        public TurnPhase Phase { get; private set; } = TurnPhase.PlayerTurn;
        public int CurrentAP { get; private set; }

        public bool IsPlayerTurn => Phase == TurnPhase.PlayerTurn;
        public bool CanPlayerAct => IsPlayerTurn && CurrentAP > 0;

        public System.Action<int, int> OnAPChanged; // (current, max)
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
            BeginPreparation();  
            UpdateLivesUI(); //sets Lives to specific amount I input
            EnsureQueenLeaderBound();
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
            // Optional: disable BoardInput here, enable it in BeginPlayerTurn.
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
            _movedThisPlayerTurn.Clear(); //set for IronMarch Queen passive
            _queenMovedThisTurn = false;
            EnsureQueenLeaderBound();
            RecomputeEnemyIntentsAndPaint();
            RepaintIronMarchHints();
        }

        public void EndPlayerTurnButton()
        {
            if (!IsPlayerTurn) return;
            // --- Iron March end-of-turn passive ---
            GrantIronMarchFortifyIfSelected();
            
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
            _moveStack.Clear();
            Phase = TurnPhase.EnemyTurn;
            
            OnPhaseChanged?.Invoke(Phase);
            
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
                Vector2Int target;
                bool haveTarget = false;

                // For Knights: first try the last highlighted (locked) tile
                if (enemy is EnemyKnight ek && ek.HasLockedIntent)
                {
                    target = ek.LockedIntent;
                    haveTarget = true;
                    ek.ClearLockedIntent(); // consume it
                }
                else
                {
                    // Others (and knight fallback): compute live as before
                    if (!enemy.TryGetComponent<IEnemyBehavior>(out var behavior))
                        continue;
                    if (!behavior.TryGetDesiredDestination(board, out var desired))
                        continue;
                    target = desired;
                    haveTarget = true;
                }

                if (haveTarget)
                {
                    // Your existing blocking/combat logic still applies here
                    TryMoveOrAttack(enemy, target);
                }


                // Small pacing pause so you can later hook animations
                if (enemyMoveDelay > 0f)
                    yield return new WaitForSeconds(enemyMoveDelay);
            }
            
            // --- Victory check (all waves started, no enemies remain) ---
            if (encounterRunner == null) encounterRunner = FindObjectOfType<EncounterRunner>();

            // Cleanup (status ticks/hazards could go here)
            Phase = TurnPhase.Cleanup;
            OnPhaseChanged?.Invoke(Phase);
            if (postEnemyTurnPause > 0f)
                yield return new WaitForSeconds(postEnemyTurnPause);
            
            if (encounterRunner != null && encounterRunner.IsVictoryReady(board))
            {
                PlayerWon();
                yield break; // stop the coroutine so we don't start a new player turn
            }
            
            //clear prior enemy intent highlights
            if (board != null) board.ClearHighlights();
            // Back to player
            BeginPlayerTurn();
        }

        /// <summary>
        /// Player-issued move (called by BoardInput). Handles AP and combat.
        /// </summary>
        public bool TryPlayerAct_Move(Piece piece, Vector2Int dest)
        {
            if (Phase != TurnPhase.PlayerTurn) return false; //checks if its players turn
            if (piece == null || board == null) return false; //check if theres no piece or board
            if (CurrentAP <= 0) return false; //check if theres available AP
            if (!board.InBounds(dest)) return false; //check if clicked destination is outta bounds

            // Snapshot mover state (for undo)
            int moverHP_before = piece.currentHP;
            bool moverPawn_hadMoved = (piece is Pawn pw) ? pw.hasMoved : false;
            var from = piece.Coord;

            // Empty tile -> relocate
            if (!board.TryGetPiece(dest, out var target))
            {
                int  fortBefore        = piece.fortifyStacks;
                bool wasMarkedMoved    = _movedThisPlayerTurn.Contains(piece);
                bool queenMovedBefore  = _queenMovedThisTurn;
                
                
                if (!board.TryMovePiece(piece, dest)) return false;
                
                // Bookkeep Iron March movement
                _movedThisPlayerTurn.Add(piece);
                piece.ClearFortify();
                if (queenLeader != null && piece == queenLeader) _queenMovedThisTurn = true;

                // Spend AP
                CurrentAP -= 1; OnAPChanged?.Invoke(CurrentAP, apPerTurn);

                // Push undo
                _moveStack.Add(new LastPlayerMove {
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

                // refresh overlays
                RecomputeEnemyIntentsAndPaint();
                return true;
            }

            // Occupied by ally -> do nothing
            if (target.Team == piece.Team) return false;

            // === PLAYER ATTACKS: no move; defender takes damage only ===
            int defHP_before = target.currentHP;

            ResolveCombat(attacker: piece, defender: target, attackerIsPlayer: true,
                out bool moverDied, out bool targetDied);

            // By design you don't kill the attacker on player turn; guard anyway
            if (moverDied) { board.RemovePiece(piece); /* AP consumed */ CurrentAP -= 1; OnAPChanged?.Invoke(CurrentAP, apPerTurn); _moveStack.Clear(); return true; }

            // If defender died, SOFT-capture (not Destroy) so we can undo
            if (targetDied)
                board.CapturePiece(target);

            // Spend AP
            CurrentAP -= 1; OnAPChanged?.Invoke(CurrentAP, apPerTurn);

            // Push undo (pure attack; no relocation)
            _moveStack.Add(new LastPlayerMove {
                mover = piece,
                from = from,
                to = from, // no movement
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

            // Empty tile -> normal move
            if (!board.TryGetPiece(to, out var target))
            {
                bool moved = board.TryMovePiece(mover, to);
                
                if (moved && Phase == TurnPhase.PlayerTurn && mover.Team == playerTeam)
                {
                    _movedThisPlayerTurn.Add(mover);
                    mover.ClearFortify();
                    if (queenLeader != null && mover == queenLeader) _queenMovedThisTurn = true;
                }


                // if an ENEMY just moved, check back-rank escape
                if (moved && Phase == TurnPhase.EnemyTurn && mover.Team == enemyTeam)
                {
                    if (EnemyReachedPlayerSide(mover))
                        LoseLifeAndDespawn(mover);
                }
                return moved;
            }
               
                

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

                    // after landing, check back-rank escape
                    if (EnemyReachedPlayerSide(mover))
                        LoseLifeAndDespawn(mover);

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
            // Base attacks
            int atkToDef = Mathf.Max(0, attacker.attack);
            int defToAtk = attackerIsPlayer ? 0 : Mathf.Max(0, defender.attack);

            // ----- Iron March: Queen adjacency buff (only if selected & queen held position) -----
            if (selectedClan == ClanType.IronMarch && queenLeader != null && !_queenMovedThisTurn)
            {
                // Is attacker an ally adjacent (8-neighborhood) to queen?
                if (attacker.Team == playerTeam)
                {
                    var dx = Mathf.Abs(attacker.Coord.x - queenLeader.Coord.x);
                    var dy = Mathf.Abs(attacker.Coord.y - queenLeader.Coord.y);
                    if (dx <= 1 && dy <= 1)
                        atkToDef += ironMarchQueenAdjacencyBonus;
                }
            }

            // ----- Apply Fortify stacks as flat damage reduction to the piece being hit -----
            // Defender takes attacker's damage minus defender's fortify
            int finalToDef = Mathf.Max(0, atkToDef - defender.fortifyStacks);

            // Attacker may take return damage (on enemy turn) reduced by attacker's own fortify
            int finalToAtk = Mathf.Max(0, defToAtk - attacker.fortifyStacks);

            defender.currentHP -= finalToDef;
            attacker.currentHP -= finalToAtk;

            attackerDied = attacker.currentHP <= 0;
            defenderDied = defender.currentHP <= 0;
        }
        
        void ComputeEnemyIntents()
        {
            _enemyIntents.Clear();
            // Only during player turn we compute/paint
            if (Phase != TurnPhase.PlayerTurn || board == null) return;

            foreach (var p in board.GetAllPieces())
            {
                if (p == null) continue;
                if (p.Team != enemyTeam) continue;

                // Skip pawns; only show "significant" pieces
                if (p is Pawn) continue;

                // Needs an enemy behavior (EncounterRunner auto-adds EnemySimpleChooser)
                if (!p.TryGetComponent<IEnemyBehavior>(out var beh)) continue;

                if (beh.TryGetDesiredDestination(board, out var dest))
                {
                    // In case behavior returns OOB, guard
                    if (board.InBounds(dest))
                        _enemyIntents.Add(dest);
                }
            }
        }
        
        //------ENEMY INTENT BEHAVIOR (Highlights the board tiles red during playerturn)----//

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

            // ✅ If all waves have been started and there are no enemies, declare victory now.
            if (encounterRunner == null) encounterRunner = FindObjectOfType<EncounterRunner>();
            if (encounterRunner != null && encounterRunner.IsVictoryReady(board))
            {
                PlayerWon(); // will raise event + show win panel + freeze turns
                return;
            }
        }
        
        //-------UNDO FEATURE (uses struct and stack to hold piece info to later call back to (FIFO)------//
        
        public bool TryUndoLastPlayerMove()
        {
            if (Phase != TurnPhase.PlayerTurn) return false;
            if (_moveStack.Count == 0 || board == null) return false;

            var m = _moveStack[_moveStack.Count - 1];

            // 1) Undo relocation if it happened
            if (m.moverActuallyMoved)
            {
                // Consistency: piece must be at 'to'
                if (board.GetPieceAt(m.to) != m.mover) { _moveStack.Clear(); return false; }

                // Move back
                board.ClearCell(m.to);
                board.PlaceWithoutCapture(m.mover, m.from);

                // Restore pawn first-move flag if applicable
                if (m.mover is Pawn pw)
                    pw.hasMoved = m.moverPawn_HadMovedBefore;
                
                // --- Restore Iron March state ---
                // Fortify stacks back to what they were before the move
                m.mover.fortifyStacks = m.moverFortify_Before;

                // Restore "moved this turn" set to its prior state
                if (m.moverWasMarkedMoved_Before)
                    _movedThisPlayerTurn.Add(m.mover);
                else
                    _movedThisPlayerTurn.Remove(m.mover);

                // Restore whether the queen had been considered moved this turn
                _queenMovedThisTurn = m.queenMovedThisTurn_Before;
            }
            else
            {
                // Pure attack: mover should remain at 'from'
                if (board.GetPieceAt(m.from) != m.mover) { _moveStack.Clear(); return false; }
            }

            // 2) Restore HP for mover
            m.mover.currentHP = m.moverHP_Before;

            // 3) If there was a defender, restore its HP and presence
            if (m.defender != null)
            {
                // If we soft-removed it, bring it back
                if (m.defenderDied)
                    board.RestoreCapturedPiece(m.defender, m.defenderAt);

                m.defender.currentHP = m.defenderHP_Before;
            }

            // 4) Refund AP and pop
            CurrentAP = Mathf.Min(CurrentAP + m.apSpent, apPerTurn);
            OnAPChanged?.Invoke(CurrentAP, apPerTurn);

            _moveStack.RemoveAt(_moveStack.Count - 1);

            // 5) Refresh overlays
            RecomputeEnemyIntentsAndPaint();
            return true;
        }
        
        //------PLAYER HEALTH (updated UI and Lives int when enemy reaches the other side of the board)-----////
        
        void UpdateLivesUI()
        {
            if (livesText != null) livesText.text = $"Lives: {playerLives}";
        }
        
        void LoseLifeAndDespawn(Piece enemy)
        {
            // Despawn first (so it doesn't act again if UI or coroutines stall)
            if (enemy != null && board != null && board.ContainsPiece(enemy))
                board.RemovePiece(enemy);

            playerLives = Mathf.Max(0, playerLives - 1);
            UpdateLivesUI();

            if (playerLives <= 0)
            {
                Debug.Log("Player defeated.");
                OnPlayerDefeated?.Invoke();

                var ui = FindObjectOfType<GameOverUI>();
                if (ui != null)
                    ui.ShowGameOver();
            }
        }
        int PlayerHomeRankY()
        {
            // Player side: y==0 if player is White, else top rank.
            return (PlayerTeam == Team.White) ? 0 : (board?.rows ?? 8) - 1;
        }
        bool EnemyReachedPlayerSide(Piece p)
        {
            if (p == null || p.Team != enemyTeam || board == null) return false;
            return p.Coord.y == PlayerHomeRankY();
        }
        
        public void RegisterLivesText(TextMeshProUGUI txt) //when UI_Battle is awoken will plug the text into the LivesTextSlot
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
            OnPlayerWon?.Invoke();   // fire the event for any listeners
            ShowWinPanel();          // pops the WinPanel UI
            // (optional) freeze turns so nothing else runs:
            Phase = TurnPhase.Cleanup;
        }
        
        //-----IRON MARCH----//
        
        void GrantIronMarchFortifyIfSelected()
        {
            if (selectedClan != ClanType.IronMarch || board == null) return;

            foreach (var p in board.GetAllPieces())
            {
                if (p == null || p.Team != playerTeam) continue;
                if (_movedThisPlayerTurn.Contains(p)) continue; // only those who held position
                p.AddFortify(ironMarchFortifyMax);
            }
        }
        
        // ReSharper disable Unity.PerformanceAnalysis
        public void RepaintIronMarchHints()
        {
            if (selectedClan != ClanType.IronMarch || board == null || queenLeader == null) return;

            // Aura only shows if Queen did NOT move this player turn
            EnsureQueenLeaderBound();
            if (queenLeader == null) return;
            if (_queenMovedThisTurn) return;

            // Adjacent-8 neighborhood
            var q = queenLeader.Coord;
            List<Vector2Int> adj = new List<Vector2Int>(8);
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                var c = new Vector2Int(q.x + dx, q.y + dy);
                if (board.InBounds(c)) adj.Add(c);
            }

            // Do not ClearHighlights() here! We want to layer this over other hints after they’re drawn.
            board.Highlight(adj, ironMarchAuraColor);
        }
        
        void EnsureQueenLeaderBound()
        {
            if (queenLeader != null && queenLeader.Board == board) return;

            // Find the player's Queen that is actually on THIS board
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




        
    }

}