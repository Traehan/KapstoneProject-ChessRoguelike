// using System.Collections;
// using System.Collections.Generic;
// using System.Linq;
// using UnityEngine;
// using TMPro;
//
// namespace Chess
// {
//     public enum TurnPhase
//     {
//         Preparation,
//         PlayerTurn,
//         EnemyTurn,
//         Cleanup
//     }
//
//     public partial class TurnManager : MonoBehaviour
//     {
//        
//         public static TurnManager Instance { get; private set; }
//         public Team PlayerTeam => playerTeam;
//
//         public TurnPhase Phase { get; private set; } = TurnPhase.PlayerTurn;
//         public int CurrentAP { get; private set; }
//
//         public bool IsPlayerTurn => Phase == TurnPhase.PlayerTurn;
//         public bool CanPlayerAct => IsPlayerTurn && CurrentAP > 0;
//
//         public HashSet<Piece> MovedThisPlayerTurnSnapshot => _movedThisPlayerTurn;
//
//         // Events
//         public System.Action<int, int> OnAPChanged;      // (current, max)
//         public System.Action<TurnPhase> OnPhaseChanged;
//         public event System.Action OnPlayerDefeated;
//         public event System.Action OnPlayerWon;
//
//         // =========================
//         //  Inspector
//         // =========================
//         [Header("Refs")]
//         [SerializeField] private ChessBoard board;
//         [SerializeField] private Team playerTeam = Team.White;
//         [SerializeField] private Team enemyTeam = Team.Black;
//         [SerializeField] private EncounterRunner encounterRunner;
//
//         [Header("Turn Pacing")]
//         [SerializeField, Range(0f, 2f)] float enemyMoveDelay = 0.35f;
//         [SerializeField, Range(0f, 2f)] float postEnemyTurnPause = 0.15f;
//
//         [Header("Turn Config")]
//         [Min(0)] public int apPerTurn = 3;
//
//         // Enemy intents
//         readonly List<Vector2Int> _enemyIntents = new();
//         readonly List<Vector2Int> _intentBuf = new();
//         readonly List<Vector2Int> _bossIntents = new();
//         [SerializeField] Color enemyIntentColor = new Color(1f, 0f, 0f, 0.85f);
//         [SerializeField] Color bossIntentColor = new Color(0.7f, 0.3f, 1f, 0.9f);
//
//         // Lives
//         [SerializeField] int playerLives = 3;
//         [SerializeField] TextMeshProUGUI livesText;
//
//         // Clan
//         [Header("Clan System")]
//         [SerializeField] ClanDefinition selectedClan;
//         [SerializeField] Queen queenLeader;
//
//         ClanRuntime _clan;
//         AbilitySO[] _abilities;
//         IronMarch_QueenAura _ironMarchAura;
//
//         // Turn bookkeeping
//         readonly HashSet<Piece> _movedThisPlayerTurn = new();
//         bool _queenMovedThisTurn;
//         
//         public void BeginEncounterFromPreparation()
//         {
//             if (Phase != TurnPhase.Preparation) return;
//             BeginPlayerTurn(); // in TurnFlow partial
//         }
//
//         public void EndPlayerTurnButton()
//         {
//             if (!IsPlayerTurn) return;
//
//             // clan abilities
//             NotifyAbilitiesEndPlayerTurn();
//
//             // piece runtimes
//             NotifyAllPlayerPieceRuntimes_EndTurn();
//
//             PaintAbilityHints();
//             StartCoroutine(EnemyTurnRoutine()); // in TurnFlow partial
//         }
//
//         public bool TrySpendAP(int amount = 1)
//         {
//             if (!IsPlayerTurn || CurrentAP < amount) return false;
//             CurrentAP -= amount;
//             OnAPChanged?.Invoke(CurrentAP, apPerTurn);
//             return true;
//         }
//
//         /// <summary> Player-issued move (called by BoardInput). Handles AP and combat. </summary>
//         public bool TryPlayerAct_Move(Piece piece, Vector2Int dest)
//         {
//             if (!ValidatePlayerMove(piece, dest)) return false;
//
//             var from = piece.Coord;
//             int moverHP_before = piece.currentHP;
//             bool pawnMovedBefore = (piece is Pawn pw) && pw.hasMoved;
//
//             // Empty tile -> relocate
//             if (!board.TryGetPiece(dest, out var target))
//             {
//                 return HandlePlayerMoveToEmpty(piece, from, dest, moverHP_before, pawnMovedBefore);
//             }
//
//             // Ally at dest -> blocked
//             if (target.Team == piece.Team) return false;
//
//             // Enemy at dest -> attack (no relocation)
//             return HandlePlayerAttack(piece, target, from, dest, moverHP_before, pawnMovedBefore);
//         }
//
//         public void RepaintEnemyIntentsOverlay()
//         {
//             if (board == null) return;
//
//             if (_enemyIntents.Count > 0)
//                 board.Highlight(_enemyIntents, enemyIntentColor);
//
//             if (_bossIntents.Count > 0)
//                 board.Highlight(_bossIntents, bossIntentColor);
//         }
//
//         public void RecomputeEnemyIntentsAndPaint()
//         {
//             if (board == null) return;
//
//             board.ClearHighlights();
//             ComputeEnemyIntents(); // in EnemyIntents partial
//             RepaintEnemyIntentsOverlay();
//
//             EnsureEncounterRunnerBound();
//             if (encounterRunner != null && encounterRunner.IsVictoryReady(board))
//                 PlayerWon(); // in VictoryLives partial
//         }
//
//         public void RegisterLivesText(TextMeshProUGUI txt)
//         {
//             livesText = txt;
//             UpdateLivesUI();
//         }
//
//         // Kept for BoardInput calls; delegates to unified ability hint painter
//         public void RepaintIronMarchHints()
//         {
//             PaintAbilityHints();
//         }
//
//         // NOTE:
//         // Restart-turn logic lives in TurnManager.RestartRound.cs.
//         // Make sure BeginPlayerTurn() calls CaptureTurnStartSnapshot() once each player turn.
//
//         // =========================
//         //  UNITY
//         // =========================
//         void Awake()
//         {
//             if (Instance != null && Instance != this)
//             {
//                 Destroy(gameObject);
//                 return;
//             }
//             Instance = this;
//         }
//
//         void Start()
//         {
//             UpdateLivesUI();
//
//             if (selectedClan == null && GameSession.I != null)
//                 selectedClan = GameSession.I.selectedClan;
//
//             EnsureQueenLeaderBound();
//             BuildClanRuntime();
//             BeginPreparation();
//         }
//     }
// }
