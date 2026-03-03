using UnityEngine;
using TMPro;
using Chess;

[DisallowMultipleComponent]
public sealed class BleedPipUI : MonoBehaviour
{
    [Header("Assign in Inspector")]
    [SerializeField] private GameObject pipPrefab; // UI_BleedPip prefab (Canvas on root, World Space)
    [SerializeField] private Transform anchor;     // StatusAnchor on the piece

    [Header("Placement")]
    [Tooltip("Extra local offset from the anchor. If you want to control position ONLY via anchor, set this to (0,0,0).")]
    [SerializeField] private Vector3 localOffset = Vector3.zero;

    [Tooltip("Controls how big the pip is in world space. Keep small (ex: 0.01).")]
    [SerializeField] private float worldScale = 0.01f;

    [Header("Facing")]
    [SerializeField] private bool faceCamera = true;
    [SerializeField] private bool lockUpright = true; // avoids tilting with camera pitch

    private GameObject _pip;
    private TextMeshProUGUI _count;
    private Piece _piece;
    private BleedStatus _bleed;
    private Camera _cam;

    void Awake()
    {
        _piece = GetComponent<Piece>();
        _bleed = GetComponent<BleedStatus>();
        _cam = Camera.main;
    }

    void OnEnable()
    {
        GameEvents.OnPieceStatsChanged += OnPieceStatsChanged;
        Refresh();
    }

    void OnDisable()
    {
        GameEvents.OnPieceStatsChanged -= OnPieceStatsChanged;
    }

    void LateUpdate()
    {
        if (!faceCamera) return;
        if (_pip == null || !_pip.activeSelf) return;

        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        var t = _pip.transform;

        if (lockUpright)
        {
            // Face camera but stay upright
            Vector3 toCam = _cam.transform.position - t.position;
            toCam.y = 0f;
            if (toCam.sqrMagnitude > 0.0001f)
                t.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
        }
        else
        {
            // Fully face camera
            t.forward = _cam.transform.forward;
        }
    }

    void OnPieceStatsChanged(Piece changed)
    {
        if (changed != _piece) return;
        Refresh();
    }

    void EnsurePip()
    {
        if (_pip != null) return;

        if (pipPrefab == null)
        {
            Debug.LogError("[BleedPipUI] pipPrefab is NULL (assign UI_BleedPip prefab).", this);
            return;
        }

        if (anchor == null)
        {
            Debug.LogWarning("[BleedPipUI] anchor is NULL (assign StatusAnchor). Using self.", this);
            anchor = transform;
        }

        _pip = Instantiate(pipPrefab, anchor);
        _pip.name = "UI_BleedPip(Clone)";

        // Position relative to the anchor
        _pip.transform.localPosition = localOffset;

        // IMPORTANT: do not inherit anchor rotation (we billboard in LateUpdate)
        _pip.transform.localRotation = Quaternion.identity;

        // ✅ IMPORTANT: cancel out the anchor's scale so anchor can be 1.2,1.2,1 and pip remains stable.
        Vector3 a = anchor.lossyScale;
        float invX = (a.x != 0f) ? 1f / a.x : 1f;
        float invY = (a.y != 0f) ? 1f / a.y : 1f;
        float invZ = (a.z != 0f) ? 1f / a.z : 1f;

        _pip.transform.localScale = new Vector3(invX, invY, invZ) * worldScale;

        _count = _pip.GetComponentInChildren<TextMeshProUGUI>(true);
        if (_count == null)
            Debug.LogError("[BleedPipUI] No TextMeshProUGUI found in pip prefab.", _pip);
    }

    public void Refresh()
    {
        if (_bleed == null) _bleed = GetComponent<BleedStatus>();

        EnsurePip();
        if (_pip == null) return;

        int stacks = (_bleed != null) ? _bleed.Stacks : 0;

        if (stacks <= 0)
        {
            _pip.SetActive(false);
            return;
        }

        _pip.SetActive(true);
        if (_count != null) _count.text = stacks.ToString();
    }
}