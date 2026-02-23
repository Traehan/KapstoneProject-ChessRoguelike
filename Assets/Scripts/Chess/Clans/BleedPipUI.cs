using UnityEngine;
using TMPro;
using Chess;

[DisallowMultipleComponent]
public sealed class BleedPipUI : MonoBehaviour
{
    [Header("Assign in Inspector")]
    [SerializeField] GameObject pipPrefab;     // UI_BleedPip prefab (Canvas on root, World Space)
    [SerializeField] Transform anchor;         // StatusAnchor on the piece

    [Header("Placement")]
    [SerializeField] Vector3 localOffset = new Vector3(0f, 0.8f, 0f);
    [SerializeField] Vector3 localEulerOffset = Vector3.zero; // optional extra rotation
    [SerializeField] float worldScale = 0.01f;

    [Header("Facing")]
    [SerializeField] bool faceCamera = true;
    [SerializeField] bool lockUpright = true; // keeps it from tilting with camera pitch

    GameObject _pip;
    TextMeshProUGUI _count;
    Piece _piece;
    BleedStatus _bleed;
    Camera _cam;

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
        if (_pip == null || !_pip.activeSelf) return;
        if (!faceCamera) return;

        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        // Billboard: face camera
        var t = _pip.transform;

        if (lockUpright)
        {
            // Face camera, but stay upright (no pitching down/up)
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

        // Apply any extra rotation you want (rarely needed)
        if (localEulerOffset != Vector3.zero)
            t.rotation *= Quaternion.Euler(localEulerOffset);
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
            Debug.LogError("[BleedPipUI] pipPrefab not assigned.", this);
            return;
        }

        if (anchor == null) anchor = transform;

        _pip = Instantiate(pipPrefab, anchor);
        _pip.transform.localPosition = localOffset;
        _pip.transform.localScale = Vector3.one * worldScale;

        // IMPORTANT: don't inherit weird rotations from anchor
        _pip.transform.localRotation = Quaternion.identity;

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