using UnityEngine;
using Card;
using Chess;

public class SpellTargetingController : MonoBehaviour
{
    public static SpellTargetingController Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private ChessBoard board;
    [SerializeField] private DeckManager deckManager;
    [SerializeField] private Camera mainCamera;

    [Header("Raycast")]
    [SerializeField] private LayerMask boardMask = ~0;

    Card.Card _selectedSpellCard;
    Piece _selectedPiece;
    bool _isTargeting;

    public bool IsTargeting => _isTargeting;
    public Card.Card SelectedSpellCard => _selectedSpellCard;
    public Piece SelectedPiece => _selectedPiece;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (turnManager == null) turnManager = TurnManager.Instance;
        if (board == null) board = FindObjectOfType<ChessBoard>();
        if (deckManager == null) deckManager = FindObjectOfType<DeckManager>();
        if (mainCamera == null) mainCamera = Camera.main;
    }

    void Update()
    {
        if (!_isTargeting)
            return;

        if (Input.GetMouseButtonDown(1))
        {
            CancelTargeting();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            HandleLeftClick();
        }
    }

    public void BeginSpellTargeting(Card.Card spellCard)
    {
        if (spellCard == null)
            return;

        if (turnManager == null || board == null || deckManager == null)
        {
            Debug.LogWarning("[SpellTargetingController] Missing refs.");
            return;
        }

        if (turnManager.Phase != TurnPhase.SpellPhase)
        {
            Debug.Log("[SpellTargetingController] Cannot cast spell outside SpellPhase.");
            return;
        }

        if (!deckManager.IsInHand(spellCard))
        {
            Debug.Log("[SpellTargetingController] Spell card is not in hand.");
            return;
        }

        if (!spellCard.IsSpellCard())
        {
            Debug.Log("[SpellTargetingController] Tried to start targeting with non-spell card.");
            return;
        }

        _selectedSpellCard = spellCard;
        _selectedPiece = null;
        _isTargeting = true;

        Debug.Log($"[SpellTargetingController] Began targeting for spell: {_selectedSpellCard.Title}");
    }

    public void CancelTargeting()
    {
        if (!_isTargeting)
            return;

        Debug.Log("[SpellTargetingController] Spell targeting cancelled.");

        _selectedSpellCard = null;
        _selectedPiece = null;
        _isTargeting = false;
    }

    void HandleLeftClick()
    {
        if (_selectedSpellCard == null)
        {
            CancelTargeting();
            return;
        }

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out var hit, 1000f, boardMask))
            return;

        if (_selectedPiece == null)
        {
            TrySelectPiece(hit);
        }
        else
        {
            TrySelectDestination(hit.point);
        }
    }

    void TrySelectPiece(RaycastHit hit)
    {
        Piece clickedPiece = hit.collider.GetComponentInParent<Piece>();
        if (clickedPiece == null)
            return;

        if (clickedPiece.Team != turnManager.PlayerTeam)
        {
            Debug.Log("[SpellTargetingController] Fortify Shift requires an allied piece.");
            return;
        }

        _selectedPiece = clickedPiece;
        Debug.Log($"[SpellTargetingController] Selected piece {_selectedPiece.name} at {_selectedPiece.Coord}");
    }

    void TrySelectDestination(Vector3 worldPoint)
    {
        if (_selectedPiece == null)
            return;

        if (!board.WorldToBoard(worldPoint, out var coord))
            return;

        int manhattan = Mathf.Abs(coord.x - _selectedPiece.Coord.x) + Mathf.Abs(coord.y - _selectedPiece.Coord.y);
        if (manhattan != 1)
        {
            Debug.Log("[SpellTargetingController] Destination must be an adjacent tile.");
            return;
        }

        if (board.TryGetPiece(coord, out _))
        {
            Debug.Log("[SpellTargetingController] Destination tile is occupied.");
            return;
        }

        var target = new FortifyShiftTarget(_selectedPiece, coord);

        var cmd = new CastSpellCardCommand(
            turnManager,
            board,
            deckManager,
            _selectedSpellCard,
            turnManager.PlayerTeam,
            target
        );

        bool success = turnManager.ExecuteCommand(cmd);

        if (success)
        {
            Debug.Log($"[SpellTargetingController] Cast {_selectedSpellCard.Title} on {_selectedPiece.name} -> {coord}");
            FindObjectOfType<HandPanel>()?.RebuildHand();
            CancelTargeting();
        }
        else
        {
            Debug.LogWarning("[SpellTargetingController] Spell cast failed.");
        }
    }
}