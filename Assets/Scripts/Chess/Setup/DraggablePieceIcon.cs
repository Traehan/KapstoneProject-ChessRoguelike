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
    public LayerMask boardMask = ~0; // optional: restrict to Tiles layer if you have one

    PieceDefinition _def;
    PlacementManager _placer;
    PrepPanel _panel;
    
    HandPanel _handPanel;
    DeckManager _deckManager;
    bool _combatMode;
    
    RectTransform _rt;
    Canvas _canvas;
    CanvasGroup _cg;
    Camera _cam;
    
    //To snap icons back to place after wrong drag
    Vector2 _startAnchoredPos;
    Transform _startParent;
    int _startSiblingIndex;

    GameObject _ghost;       // world-space ghost parent
    Piece _ghostPiece;
    Vector2Int _snapCoord;
    bool _canPlaceHere;
    static Plane _boardPlane;   // fallback when no collider was hit

    public void Init(PieceDefinition def, PlacementManager placer, PrepPanel panel)
    {
        _def = def;
        _placer = placer;
        _panel = panel;
        GetComponent<Image>().sprite = def.icon;
    }
    
    public void InitForCombat(PieceDefinition def, PlacementManager placer, HandPanel handPanel, DeckManager deckManager)
    {
        _combatMode = true;
        _def = def;
        _placer = placer;
        _handPanel = handPanel;
        _deckManager = deckManager;

        GetComponent<Image>().sprite = def.icon;
    }

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _cg = GetComponent<CanvasGroup>();
        _canvas = GetComponentInParent<Canvas>();
        _cam = Camera.main;
    }

    public void OnBeginDrag(PointerEventData e)
    {
        _startAnchoredPos = _rt.anchoredPosition;
        _startParent = _rt.parent;
        _startSiblingIndex = _rt.GetSiblingIndex();
        _cg.blocksRaycasts = false;   // let pointer reach world
        _ghost = null;
        _ghostPiece = null;

        if (_placer && _placer.board)
            _boardPlane = new Plane(_placer.board.transform.up, _placer.board.transform.position);
    }

    public void OnDrag(PointerEventData e)
    {
        // Move icon with cursor for UI feedback
        _rt.anchoredPosition += e.delta / _canvas.scaleFactor;

        if (_placer == null || _placer.board == null) return;
        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        var ray = _cam.ScreenPointToRay(e.position);

// 1) Try to hit colliders (tiles)
        Vector3 worldPoint = default;   // <-- initialize it
        bool gotPoint = false;

// Use either ALL positional args or ALL named args, not mixed.
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, boardMask))   // positional ok
        {
            worldPoint = hit.point;
            gotPoint = true;
        }
        else
        {
            // 2) Fallback to an infinite plane at board height
            if (_boardPlane.Raycast(ray, out float enter))
            {
                worldPoint = ray.GetPoint(enter);
                gotPoint = true;
            }
        }

        if (!gotPoint) return;   // guarantees worldPoint is set before use


        // Convert to board coord
        if (_placer.board.WorldToBoard(worldPoint, out var c))
        {
            // Lazy-build ghost the first time we have a valid coord
            if (_ghostPiece == null)
            {
                if (_def.piecePrefab == null) return;
                
                _ghostPiece = Instantiate(_def.piecePrefab, _placer.board.transform);
                _ghost = _ghostPiece.gameObject;
                _ghostPiece.Init(_placer.board, _placer.playerTeam, c);
                SetGhostVisual(0.5f, true);

                // IMPORTANT: so the ghost never blocks our raycasts
                SetLayerRecursive(_ghost.transform, LayerMask.NameToLayer("Ignore Raycast"));
                DisableColliders(_ghost, true);
            }

            // Snap ghost to tile center
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
                // Combat: execute an undoable command (AP + placement + Hand->Exhaust)
                if (tm != null && _deckManager != null && _placer != null && _placer.board != null)
                {
                    var cmd = new Chess.PlayCardPlaceCommand(
                        tm,
                        _placer.board,
                        _placer,
                        _deckManager,
                        _def,
                        _snapCoord,
                        apCost: 1
                    );

                    placed = tm.ExecuteCommand(cmd);
                }
            }
            else
            {
                // Prep: no AP, just place
                placed = _placer.TryPlace(_def, _snapCoord);
            }
        }

        if (placed)
        {
            if (_combatMode)
            {
                // remove icon from hand UI (deck changes were done inside the command)
                _handPanel?.OnCardPlayed(this);

                // refresh hand to reflect updated Hand list
                FindObjectOfType<HandPanel>()?.RebuildHand();
            }
            else
            {
                // prep behavior: consume icon
                _panel.OnIconConsumed(this);
            }
        }
        else
        {
            // Snap back to original spot in the hand UI
            _rt.SetParent(_startParent);
            _rt.SetSiblingIndex(_startSiblingIndex);
            _rt.anchoredPosition = _startAnchoredPos;
        }

        // Always clean up the ghost
        if (_ghost) Destroy(_ghost);
        _ghostPiece = null;
        _ghost = null;
    }

    // ===== helpers =====

    void SetGhostVisual(float alpha, bool includeChildren)
    {
        var rends = includeChildren ? _ghost.GetComponentsInChildren<Renderer>() : _ghost.GetComponents<Renderer>();
        foreach (var r in rends)
        {
            var mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);
            // try common color properties
            if (r.sharedMaterial.HasProperty("_BaseColor"))
            {
                var c = r.sharedMaterial.GetColor("_BaseColor"); c.a = alpha;
                mpb.SetColor("_BaseColor", c);
            }
            if (r.sharedMaterial.HasProperty("_Color"))
            {
                var c = r.sharedMaterial.GetColor("_Color"); c.a = alpha;
                mpb.SetColor("_Color", c);
            }
            r.SetPropertyBlock(mpb);
        }
    }

    void TintGhost(bool ok)
    {
        var color = ok ? new Color(0f, 1f, 0f, 0.6f) : new Color(1f, 0f, 0f, 0.6f);
        var rends = _ghost.GetComponentsInChildren<Renderer>();
        foreach (var r in rends)
        {
            var mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);
            if (r.sharedMaterial.HasProperty("_BaseColor")) mpb.SetColor("_BaseColor", color);
            if (r.sharedMaterial.HasProperty("_Color")) mpb.SetColor("_Color", color);
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
        foreach (var c in root.GetComponentsInChildren<Collider>())
            c.enabled = !value;
    }
}
