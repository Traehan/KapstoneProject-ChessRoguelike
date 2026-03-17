using System.Collections;
using System.Collections.Generic;
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

    [Header("Highlight Colors")]
    [SerializeField] private Color validTargetColor = new Color(0.2f, 0.7f, 1f, 0.45f);
    [SerializeField] private Color selectedPieceColor = new Color(1f, 0.8f, 0.1f, 0.55f);
    [SerializeField] private Color validDestinationColor = new Color(0.2f, 1f, 0.4f, 0.45f);
    [SerializeField] private Color successFlashColor = new Color(0.2f, 1f, 0.2f, 0.8f);

    [Header("Timing")]
    [SerializeField] private float successFlashDuration = 1f;

    Card.Card _selectedSpellCard;
    Piece _selectedPiece;
    bool _isTargeting;
    Coroutine _flashRoutine;

    public bool IsTargeting => _isTargeting;
    public Card.Card SelectedSpellCard => _selectedSpellCard;
    public Piece SelectedPiece => _selectedPiece;
    
    public System.Action<Card.Card, bool> OnSpellTargetingStateChanged;

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
            HandleLeftClick();
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
        
        if (_isTargeting && _selectedSpellCard == spellCard) //make sure spell card only raises once
            return;

        if (_selectedSpellCard != null && _selectedSpellCard != spellCard)
            OnSpellTargetingStateChanged?.Invoke(_selectedSpellCard, false);
        
        _selectedSpellCard = spellCard;
        _selectedPiece = null;
        _isTargeting = true;

        OnSpellTargetingStateChanged?.Invoke(_selectedSpellCard, true);
        
        RefreshHighlights();
        Debug.Log($"[SpellTargetingController] Began targeting for spell: {_selectedSpellCard.Title}");
    }

    public void CancelTargeting()
    {
        if (!_isTargeting)
            return;
        
        var oldCard = _selectedSpellCard;

        _selectedSpellCard = null;
        _selectedPiece = null;
        _isTargeting = false;
        
        OnSpellTargetingStateChanged?.Invoke(oldCard, false);

        ClearSpellHighlights();
        Debug.Log("[SpellTargetingController] Spell targeting cancelled.");
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
            TrySelectFirstTarget(hit);
        else
            TrySelectSecondTarget(hit);
    }

    void TrySelectFirstTarget(RaycastHit hit)
    {
        Piece clickedPiece = hit.collider.GetComponentInParent<Piece>();
        if (clickedPiece == null)
            return;

        if (_selectedSpellCard == null)
            return;

        var spellDef = _selectedSpellCard.Definition as SpellCardDefinitionSO;
        if (spellDef == null)
            return;

        switch (spellDef.targetingMode)
        {
            case CardTargetingMode.AlliedPiece:
            {
                if (clickedPiece.Team != turnManager.PlayerTeam)
                {
                    Debug.Log("[SpellTargetingController] Must select an allied piece.");
                    return;
                }

                ExecuteSpell(clickedPiece, clickedPiece.Coord);
                break;
            }

            case CardTargetingMode.EnemyPiece:
            {
                if (clickedPiece.Team == turnManager.PlayerTeam)
                {
                    Debug.Log("[SpellTargetingController] Must select an enemy piece.");
                    return;
                }

                ExecuteSpell(clickedPiece, clickedPiece.Coord);
                break;
            }

            case CardTargetingMode.AnyPiece:
            {
                ExecuteSpell(clickedPiece, clickedPiece.Coord);
                break;
            }

            case CardTargetingMode.AlliedPieceThenAdjacentEmptyTile:
            case CardTargetingMode.AlliedPieceThenAlliedPiece:
            case CardTargetingMode.AlliedPieceThenAdjacentPiece:
            {
                if (clickedPiece.Team != turnManager.PlayerTeam)
                {
                    Debug.Log("[SpellTargetingController] Must select an allied piece.");
                    return;
                }

                _selectedPiece = clickedPiece;
                RefreshHighlights();
                Debug.Log($"[SpellTargetingController] Selected first piece {_selectedPiece.name}");
                break;
            }
        }
    }

    void TrySelectSecondTarget(RaycastHit hit)
    {
        if (_selectedPiece == null || _selectedSpellCard == null)
            return;

        var spellDef = _selectedSpellCard.Definition as SpellCardDefinitionSO;
        if (spellDef == null)
            return;

        switch (spellDef.targetingMode)
        {
            case CardTargetingMode.AlliedPieceThenAdjacentEmptyTile:
            {
                if (!board.WorldToBoard(hit.point, out var coord))
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
                ExecuteSpell(target, coord);
                break;
            }

            case CardTargetingMode.AlliedPieceThenAlliedPiece:
            {
                Piece clickedPiece = hit.collider.GetComponentInParent<Piece>();
                if (clickedPiece == null)
                    return;

                if (clickedPiece.Team != turnManager.PlayerTeam)
                {
                    Debug.Log("[SpellTargetingController] Second target must be an allied piece.");
                    return;
                }

                if (clickedPiece == _selectedPiece)
                {
                    Debug.Log("[SpellTargetingController] Choose a different allied piece.");
                    return;
                }

                var target = new TransferFortifyTarget(_selectedPiece, clickedPiece);
                ExecuteSpell(target, clickedPiece.Coord);
                break;
            }

            case CardTargetingMode.AlliedPieceThenAdjacentPiece:
            {
                Piece clickedPiece = hit.collider.GetComponentInParent<Piece>();
                if (clickedPiece == null)
                    return;

                if (clickedPiece == _selectedPiece)
                {
                    Debug.Log("[SpellTargetingController] Choose a different adjacent piece.");
                    return;
                }

                int manhattan = Mathf.Abs(clickedPiece.Coord.x - _selectedPiece.Coord.x)
                              + Mathf.Abs(clickedPiece.Coord.y - _selectedPiece.Coord.y);

                if (manhattan != 1)
                {
                    Debug.Log("[SpellTargetingController] Second piece must be adjacent.");
                    return;
                }

                var target = new PhalanxRotateTarget(_selectedPiece, clickedPiece);
                ExecuteSpell(target, clickedPiece.Coord);
                break;
            }
        }
    }

    void ExecuteSpell(object target, Vector2Int flashCoord)
    {
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
            StartSuccessFlash(flashCoord);
            FindObjectOfType<HandPanel>()?.RebuildHand();
            CancelTargeting();
        }
        else
        {
            Debug.LogWarning("[SpellTargetingController] Spell cast failed.");
        }
    }

    void RefreshHighlights()
    {
        ClearSpellHighlights();

        if (_selectedSpellCard == null || board == null)
            return;

        var spellDef = _selectedSpellCard.Definition as SpellCardDefinitionSO;
        if (spellDef == null)
            return;

        if (_selectedPiece == null)
        {
            switch (spellDef.targetingMode)
            {
                case CardTargetingMode.AlliedPiece:
                    board.Highlight(GetTargetablePieceCoords(alliedOnly: true), validTargetColor);
                    break;

                case CardTargetingMode.EnemyPiece:
                    board.Highlight(GetTargetablePieceCoords(enemyOnly: true), validTargetColor);
                    break;

                case CardTargetingMode.AnyPiece:
                    board.Highlight(GetTargetablePieceCoords(anyPiece: true), validTargetColor);
                    break;

                case CardTargetingMode.AlliedPieceThenAdjacentEmptyTile:
                case CardTargetingMode.AlliedPieceThenAlliedPiece:
                case CardTargetingMode.AlliedPieceThenAdjacentPiece:
                    board.Highlight(GetTargetablePieceCoords(alliedOnly: true), validTargetColor);
                    break;
            }
        }
        else
        {
            board.Highlight(new[] { _selectedPiece.Coord }, selectedPieceColor);

            switch (spellDef.targetingMode)
            {
                case CardTargetingMode.AlliedPieceThenAdjacentEmptyTile:
                    board.Highlight(GetAdjacentEmptyCoords(_selectedPiece.Coord), validDestinationColor);
                    break;

                case CardTargetingMode.AlliedPieceThenAlliedPiece:
                    board.Highlight(GetTargetableAlliedPieceCoordsExcept(_selectedPiece), validDestinationColor);
                    break;

                case CardTargetingMode.AlliedPieceThenAdjacentPiece:
                    board.Highlight(GetAdjacentPieceCoords(_selectedPiece.Coord, includeSelected: false), validDestinationColor);
                    break;
            }
        }
    }

    void ClearSpellHighlights()
    {
        if (board == null) return;

        board.ClearHighlights();
        turnManager?.RecomputeEnemyIntentsAndPaint();
    }

    List<Vector2Int> GetTargetablePieceCoords(bool alliedOnly = false, bool enemyOnly = false, bool anyPiece = false)
    {
        var coords = new List<Vector2Int>();
        if (board == null || turnManager == null)
            return coords;

        foreach (var piece in board.GetAllPieces())
        {
            if (piece == null) continue;

            if (anyPiece)
            {
                coords.Add(piece.Coord);
                continue;
            }

            if (alliedOnly && piece.Team == turnManager.PlayerTeam)
                coords.Add(piece.Coord);

            if (enemyOnly && piece.Team != turnManager.PlayerTeam)
                coords.Add(piece.Coord);
        }

        return coords;
    }

    List<Vector2Int> GetTargetableAlliedPieceCoordsExcept(Piece excluded)
    {
        var coords = new List<Vector2Int>();
        if (board == null || turnManager == null)
            return coords;

        foreach (var piece in board.GetAllPieces())
        {
            if (piece == null) continue;
            if (piece == excluded) continue;
            if (piece.Team != turnManager.PlayerTeam) continue;
            coords.Add(piece.Coord);
        }

        return coords;
    }

    List<Vector2Int> GetAdjacentEmptyCoords(Vector2Int center)
    {
        var results = new List<Vector2Int>();
        if (board == null) return results;

        Vector2Int[] dirs =
        {
            new Vector2Int( 1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int( 0, 1),
            new Vector2Int( 0,-1)
        };

        for (int i = 0; i < dirs.Length; i++)
        {
            var c = center + dirs[i];
            if (!board.InBounds(c)) continue;
            if (board.TryGetPiece(c, out _)) continue;
            results.Add(c);
        }

        return results;
    }

    List<Vector2Int> GetAdjacentPieceCoords(Vector2Int center, bool includeSelected = false)
    {
        var results = new List<Vector2Int>();
        if (board == null) return results;

        Vector2Int[] dirs =
        {
            new Vector2Int( 1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int( 0, 1),
            new Vector2Int( 0,-1)
        };

        for (int i = 0; i < dirs.Length; i++)
        {
            var c = center + dirs[i];
            if (!board.InBounds(c)) continue;
            if (!board.TryGetPiece(c, out var p)) continue;
            if (!includeSelected && p == _selectedPiece) continue;
            results.Add(c);
        }

        return results;
    }

    void StartSuccessFlash(Vector2Int coord)
    {
        if (_flashRoutine != null)
            StopCoroutine(_flashRoutine);

        _flashRoutine = StartCoroutine(SuccessFlashRoutine(coord));
    }

    IEnumerator SuccessFlashRoutine(Vector2Int coord)
    {
        if (board == null) yield break;

        board.ClearHighlights();
        board.Highlight(new[] { coord }, successFlashColor);
        yield return new WaitForSeconds(successFlashDuration);

        _flashRoutine = null;

        if (_isTargeting)
            RefreshHighlights();
        else
            turnManager?.RecomputeEnemyIntentsAndPaint();
    }
}