using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Chess;
using Card;

[RequireComponent(typeof(Image), typeof(CanvasGroup))]
public class DraggablePieceIcon : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Raycast")]
    public LayerMask boardMask = ~0;

    PieceDefinition _def;
    Card.Card _card;

    PlacementManager _placer;
    PrepPanel _panel;

    HandPanel _handPanel;
    DeckManager _deckManager;
    bool _combatMode;

    RectTransform _rt;
    Canvas _canvas;
    CanvasGroup _cg;
    Camera _cam;

    Vector2 _startAnchoredPos;
    Transform _startParent;
    int _startSiblingIndex;

    GameObject _ghost;
    Piece _ghostPiece;
    Vector2Int _snapCoord;
    bool _canPlaceHere;
    static Plane _boardPlane;

    CanvasGroup canvasGroup;

    public void Init(PieceDefinition def, PlacementManager placer, PrepPanel panel)
    {
        _combatMode = false;
        _card = null;
        _def = def;
        _placer = placer;
        _panel = panel;

        var image = GetComponent<Image>();
        if (image != null)
            image.sprite = def != null ? def.icon : null;
    }

    // Compatibility overload so existing HandPanel calls still compile
    public void InitForCombat(PieceDefinition def, PlacementManager placer, HandPanel handPanel, DeckManager deckManager)
    {
        _combatMode = true;
        _def = def;
        _card = null;
        _placer = placer;
        _handPanel = handPanel;
        _deckManager = deckManager;

        var image = GetComponent<Image>();
        if (image != null)
            image.sprite = def != null ? def.icon : null;
    }

    // Preferred overload for the new card system
    public void InitForCombat(Card.Card card, PlacementManager placer, HandPanel handPanel, DeckManager deckManager)
    {
        _combatMode = true;
        _card = card;
        _def = card != null ? card.GetSummonPieceDefinition() : null;
        _placer = placer;
        _handPanel = handPanel;
        _deckManager = deckManager;

        var image = GetComponent<Image>();
        if (image != null)
            image.sprite = card != null ? card.Art : (_def != null ? _def.icon : null);
    }

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _cg = GetComponent<CanvasGroup>();
        _canvas = GetComponentInParent<Canvas>();
        _cam = Camera.main;
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData e)
    {
        _startAnchoredPos = _rt.anchoredPosition;
        _startParent = _rt.parent;
        _startSiblingIndex = _rt.GetSiblingIndex();
        _cg.blocksRaycasts = false;
        _ghost = null;
        _ghostPiece = null;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.45f;
            canvasGroup.blocksRaycasts = false;
        }

        if (_placer && _placer.board)
            _boardPlane = new Plane(_placer.board.transform.up, _placer.board.transform.position);
    }

    public void OnDrag(PointerEventData e)
    {
        _rt.anchoredPosition += e.delta / _canvas.scaleFactor;

        if (_placer == null || _placer.board == null) return;
        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;
        if (_def == null) return;

        var ray = _cam.ScreenPointToRay(e.position);

        Vector3 worldPoint = default;
        bool gotPoint = false;

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, boardMask))
        {
            worldPoint = hit.point;
            gotPoint = true;
        }
        else if (_boardPlane.Raycast(ray, out float enter))
        {
            worldPoint = ray.GetPoint(enter);
            gotPoint = true;
        }

        if (!gotPoint) return;

        if (_placer.board.WorldToBoard(worldPoint, out var c))
        {
            if (_ghostPiece == null)
            {
                if (_def.piecePrefab == null) return;

                _ghostPiece = Instantiate(_def.piecePrefab, _placer.board.transform);
                _ghost = _ghostPiece.gameObject;
                _ghostPiece.Init(_placer.board, _placer.playerTeam, c);
                SetGhostVisual(0.5f, true);

                int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
                if (ignoreRaycastLayer >= 0)
                    SetLayerRecursive(_ghost.transform, ignoreRaycastLayer);

                DisableColliders(_ghost, true);
            }

            _ghostPiece.ApplyBoardMove(c);
            _snapCoord = c;

            _canPlaceHere = _placer.CanPlace(_def, c);
            TintGhost(_canPlaceHere);
        }
    }

    public void OnEndDrag(PointerEventData e)
    {
        _cg.blocksRaycasts = true;

        bool placed = false;
        var tm = TurnManager.Instance;

        if (_ghostPiece != null && _canPlaceHere)
        {
            if (_combatMode)
            {
                if (tm != null &&
                    tm.Phase == TurnPhase.SpellPhase &&
                    _deckManager != null &&
                    _placer != null &&
                    _placer.board != null)
                {
                    var cardToPlay = ResolveCombatCard();

                    if (cardToPlay != null)
                    {
                        var cmd = new Chess.PlayCardPlaceCommand(
                            tm,
                            _placer.board,
                            _placer,
                            _deckManager,
                            cardToPlay,
                            _snapCoord
                        );

                        placed = tm.ExecuteCommand(cmd);
                    }
                }
            }
            else
            {
                placed = _placer.TryPlace(_def, _snapCoord);
            }
        }

        if (placed)
        {
            if (_combatMode)
            {
                _handPanel?.OnCardPlayed(this);
                FindObjectOfType<HandPanel>()?.RebuildHand();
            }
            else
            {
                _panel?.OnIconConsumed(this);
            }
        }
        else
        {
            _rt.SetParent(_startParent);
            _rt.SetSiblingIndex(_startSiblingIndex);
            _rt.anchoredPosition = _startAnchoredPos;
        }

        if (_ghost) Destroy(_ghost);
        _ghostPiece = null;
        _ghost = null;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }
    }

    Card.Card ResolveCombatCard()
    {
        if (_card != null)
            return _card;

        if (_deckManager == null || _def == null)
            return null;

        // Compatibility fallback:
        // find the first unit card in hand that summons this same piece definition
        var hand = _deckManager.Hand;
        if (hand == null) return null;

        for (int i = 0; i < hand.Count; i++)
        {
            var candidate = hand[i];
            if (candidate == null) continue;

            var summonDef = candidate.GetSummonPieceDefinition();
            if (summonDef == _def)
                return candidate;
        }

        Debug.LogWarning("[DraggablePieceIcon] Could not resolve a runtime Card from hand for this dragged unit icon.");
        return null;
    }

    void SetGhostVisual(float alpha, bool includeChildren)
    {
        if (_ghost == null) return;

        var rends = includeChildren
            ? _ghost.GetComponentsInChildren<Renderer>()
            : _ghost.GetComponents<Renderer>();

        foreach (var r in rends)
        {
            if (r == null || r.sharedMaterial == null) continue;

            var mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);

            if (r.sharedMaterial.HasProperty("_BaseColor"))
            {
                var c = r.sharedMaterial.GetColor("_BaseColor");
                c.a = alpha;
                mpb.SetColor("_BaseColor", c);
            }

            if (r.sharedMaterial.HasProperty("_Color"))
            {
                var c = r.sharedMaterial.GetColor("_Color");
                c.a = alpha;
                mpb.SetColor("_Color", c);
            }

            r.SetPropertyBlock(mpb);
        }
    }

    void TintGhost(bool ok)
    {
        if (_ghost == null) return;

        var color = ok ? new Color(0f, 1f, 0f, 0.6f) : new Color(1f, 0f, 0f, 0.6f);
        var rends = _ghost.GetComponentsInChildren<Renderer>();

        foreach (var r in rends)
        {
            if (r == null || r.sharedMaterial == null) continue;

            var mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);

            if (r.sharedMaterial.HasProperty("_BaseColor"))
                mpb.SetColor("_BaseColor", color);

            if (r.sharedMaterial.HasProperty("_Color"))
                mpb.SetColor("_Color", color);

            r.SetPropertyBlock(mpb);
        }
    }

    static void SetLayerRecursive(Transform t, int layer)
    {
        t.gameObject.layer = layer;
        for (int i = 0; i < t.childCount; i++)
            SetLayerRecursive(t.GetChild(i), layer);
    }

    static void DisableColliders(GameObject root, bool value)
    {
        if (root == null) return;

        foreach (var c in root.GetComponentsInChildren<Collider>())
            c.enabled = !value;
    }
}