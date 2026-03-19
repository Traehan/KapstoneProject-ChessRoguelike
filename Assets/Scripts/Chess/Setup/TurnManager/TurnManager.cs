using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using Card;

namespace Chess
{
    public enum TurnPhase
    {
        Preparation,
        SpellPhase,
        PlayerTurn,
        EnemyTurn,
        Cleanup
    }

    public partial class TurnManager : MonoBehaviour
    {
        public static TurnManager Instance { get; private set; }
        CommandHistory _history = new CommandHistory();
        public Team PlayerTeam => playerTeam;
        public TurnPhase Phase { get; private set; } = TurnPhase.PlayerTurn;
        //AP INFO
        public int CurrentAP { get; private set; }
        public int CurrentAPMax { get; private set; }
        int _pendingNextBattlePhaseAPBonus;
        public bool IsPlayerTurn => Phase == TurnPhase.PlayerTurn;
        public bool CanPlayerAct => IsPlayerTurn && CurrentAP > 0;
        public HashSet<Piece> MovedThisPlayerTurnSnapshot => _movedThisPlayerTurn;
        public System.Action<int, int> OnAPChanged;
        public System.Action<TurnPhase> OnPhaseChanged;
        
        //MANA INFO
        [Header("Mana (Spell Phase)")]
        [SerializeField] int manaPerSpellPhase = 3;
        [SerializeField] int maxMana = 5;
        public int CurrentMana { get; private set; }
        public int CurrentManaMax { get; private set; }
        public int MaxMana => maxMana;
        int _pendingNextSpellPhaseManaBonus;
        public System.Action<int, int> OnManaChanged;
        public bool IsSpellPhase => Phase == TurnPhase.SpellPhase;
        public bool ExecuteCommand(IGameCommand cmd) => _history.Execute(cmd);
        public event System.Action OnPlayerWon;
        public bool WasMarkedMovedThisTurn(Piece p) => _movedThisPlayerTurn.Contains(p);
        public void MarkMovedThisTurn(Piece p) { if (p != null) _movedThisPlayerTurn.Add(p); }
        public void UnmarkMovedThisTurn(Piece p) { if (p != null) _movedThisPlayerTurn.Remove(p); }
        public bool GetQueenMovedThisTurn() => _queenMovedThisTurn;
        public void SetQueenMovedThisTurn(bool v) => _queenMovedThisTurn = v;
        public bool IsQueenLeader(Piece p) => p != null && queenLeader != null && p == queenLeader;

        [Header("Refs")]
        [SerializeField] private ChessBoard board;
        [SerializeField] private Team playerTeam = Team.White;
        [SerializeField] private Team enemyTeam = Team.Black;
        [SerializeField] private EncounterRunner encounterRunner;

        [Header("Turn Pacing")]
        [SerializeField, Range(0f, 2f)] float enemyMoveDelay = 0.35f;
        [SerializeField, Range(0f, 2f)] float postEnemyTurnPause = 0.15f;

        [Header("Turn Config")]
        [Min(0)] public int apPerTurn = 3;

        [Header("Card System")]
        [SerializeField] private DeckManager deckManager;

        readonly List<Vector2Int> _enemyIntents = new();
        readonly List<Vector2Int> _intentBuf = new();
        readonly List<Vector2Int> _bossIntents = new();
        [SerializeField] Color enemyIntentColor = new Color(1f, 0f, 0f, 0.85f);
        [SerializeField] Color bossIntentColor = new Color(0.7f, 0.3f, 1f, 0.9f);

        [SerializeField] int playerLives = 3;
        [SerializeField] TextMeshProUGUI livesText;

        [Header("Clan System")]
        [SerializeField] ClanDefinition selectedClan;
        [SerializeField] Queen queenLeader;

        ClanRuntime _clan;
        AbilitySO[] _abilities;
        IronMarch_QueenAura _ironMarchAura;

        readonly HashSet<Piece> _movedThisPlayerTurn = new();
        bool _queenMovedThisTurn;
        
        void RaiseAPChanged()
        {
            OnAPChanged?.Invoke(CurrentAP, CurrentAPMax);
            GameEvents.OnAPChanged?.Invoke(CurrentAP, CurrentAPMax);
        }

        public void GrantNextBattlePhaseAP(int amount)
        {
            if (amount <= 0) return;
            _pendingNextBattlePhaseAPBonus += amount;
        }

        public void RemovePendingNextBattlePhaseAP(int amount)
        {
            if (amount <= 0) return;
            _pendingNextBattlePhaseAPBonus = Mathf.Max(0, _pendingNextBattlePhaseAPBonus - amount);
        }

        public void RefundAP(int amount)
        {
            if (amount <= 0) return;
            CurrentAP = Mathf.Min(CurrentAP + amount, CurrentAPMax);
            RaiseAPChanged();
        }

        public bool TrySpendAP(int amount = 1)
        {
            if (!IsPlayerTurn || CurrentAP < amount) return false;
            CurrentAP -= amount;
            RaiseAPChanged();
            return true;
        }

        public void EndPlayerTurnButton()
        {
            if (!IsPlayerTurn) return;

            deckManager?.DiscardEndOfTurn();

            NotifyAbilitiesEndPlayerTurn();
            NotifyAllPlayerPieceRuntimes_EndTurn();

            PaintAbilityHints();
            StatusTickSystem.TickEndOfPlayerTurn(board);
            StartCoroutine(EnemyTurnRoutine());
        }

        public bool TryPlayerAct_Move(Piece piece, Vector2Int dest)
        {
            if (!ValidatePlayerMove(piece, dest)) return false;

            var from = piece.Coord;

            if (!board.TryGetPiece(dest, out var target))
                return _history.Execute(new MoveCommand(this, board, piece, from, dest, apCost: 1));

            if (target.Team == piece.Team) return false;

            return _history.Execute(new AttackCommand(this, board, piece, target, from, dest, apCost: 1));
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
            ComputeEnemyIntents();
            RepaintEnemyIntentsOverlay();

            EnsureEncounterRunnerBound();
            if (encounterRunner != null && encounterRunner.IsVictoryReady(board))
                PlayerWon();
        }

        public void RegisterLivesText(TextMeshProUGUI txt)
        {
            livesText = txt;
            UpdateLivesUI();
        }

        public void RepaintIronMarchHints()
        {
            PaintAbilityHints();
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            CurrentAPMax = apPerTurn;
            CurrentManaMax = Mathf.Min(manaPerSpellPhase, maxMana);
        }

        void Start()
        {
            UpdateLivesUI();

            if (selectedClan == null && GameSession.I != null)
                selectedClan = GameSession.I.selectedClan;

            EnsureQueenLeaderBound();
            BuildClanRuntime();
            BeginPreparation();
        }
        
        void RaiseManaChanged()
        {
            OnManaChanged?.Invoke(CurrentMana, CurrentManaMax);
            GameEvents.OnManaChanged?.Invoke(CurrentMana, CurrentManaMax);
        }

        public void RefillManaForSpellPhase()
        {
            int pending = Mathf.Max(0, _pendingNextSpellPhaseManaBonus);

            // Same style as AP: this phase gets a bigger mana budget if a bonus was queued.
            CurrentManaMax = Mathf.Min(manaPerSpellPhase + pending, maxMana + pending);
            CurrentMana = CurrentManaMax;

            _pendingNextSpellPhaseManaBonus = 0;
            RaiseManaChanged();
        }

        public bool TrySpendMana(int amount)
        {
            if (!IsSpellPhase) return false;
            if (amount <= 0) return true;
            if (CurrentMana < amount) return false;

            CurrentMana -= amount;
            RaiseManaChanged();
            return true;
        }

        public void GrantNextSpellPhaseMana(int amount)
        {
            if (amount <= 0) return;
            _pendingNextSpellPhaseManaBonus += amount;
        }

        public void RemovePendingNextSpellPhaseMana(int amount)
        {
            if (amount <= 0) return;
            _pendingNextSpellPhaseManaBonus = Mathf.Max(0, _pendingNextSpellPhaseManaBonus - amount);
        }

        public void RefundMana(int amount)
        {
            if (amount <= 0) return;
            CurrentMana = Mathf.Min(CurrentMana + amount, CurrentManaMax);
            RaiseManaChanged();
        }
    }
}