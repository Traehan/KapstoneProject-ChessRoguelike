// Assets/Enemies/EncounterSetup/Map3D/MapCameraRig.cs
//
// Follow + free-look controller for the 3D map camera. Attach directly to the map's camera GameObject
// (the rig IS the camera transform - no extra parent object needed).
//
// Default behavior: lerps to target.position + followOffset with a fixed tilt (matching the battle
// board camera's look). Right-mouse-drag pans a free-look focus point (clamped to the map's bounds),
// scroll wheel zooms; either picking a new node (NotifyNodeSelected) or an idle timeout snaps back to
// follow mode. SnapToToken() is used on scene load/restore for an instant cut with no lerp.
//
// NOTE: default followOffset/fixedEulerAngles below are sized to show ~4 rows of depth by default
// (matching tileSpacingZ=10 in MapGenerator - 4 rows = 40 units) before free-look is needed to see
// further up the map. For a fixed-rotation camera on a flat ground plane, the visible ground depth is
// governed by height/tilt/FOV alone (not by how far back the camera sits): with vertical FOV F and tilt
// angle T (degrees below horizontal), the near/far ground intersections of the view cone sit at
// height/tan(T + F/2) and height/tan(T - F/2) forward of the point directly below the camera. Changing
// tileSpacingZ, FOV, or how many rows should show requires re-deriving these two fields together - don't
// tune tilt/height independently without checking the other.

using UnityEngine;

public class MapCameraRig : MonoBehaviour
{
    enum Mode
    {
        Follow,
        FreeLook
    }

    [Header("Follow")]
    [Tooltip("Usually the MapClanToken's transform.")]
    public Transform target;
    [Tooltip("Camera position = focus point + this offset. Height 22 + tilt 58 degrees is sized to show ~4 rows at tileSpacingZ=10 - see the class-level comment before changing.")]
    public Vector3 followOffset = new Vector3(0f, 22f, -3f);
    [Tooltip("Fixed camera tilt - X=58 is paired with followOffset.y=22 to show ~4 rows of depth. See the class-level comment.")]
    public Vector3 fixedEulerAngles = new Vector3(58f, 0f, 0f);
    [SerializeField, Min(0.01f)] float followLerpSpeed = 6f;

    [Header("Free-Look Pan (right-mouse drag)")]
    [Tooltip("World units of pan per screen pixel of mouse movement while dragging.")]
    [SerializeField] float panSpeed = 0.08f;
    [SerializeField] float boundsMargin = 4f;

    [Header("Free-Look Zoom (scroll wheel)")]
    [SerializeField] float zoomSpeed = 1f;
    [SerializeField] float minZoomScale = 0.6f;
    [SerializeField] float maxZoomScale = 1.8f;

    [Header("Auto-Return")]
    [Tooltip("Seconds of no free-look input before snapping back to follow mode.")]
    [SerializeField] float idleReturnDelay = 2.5f;

    Mode _mode = Mode.Follow;
    Vector3 _focusPoint;
    float _zoomScale = 1f;
    float _idleTimer;

    bool _boundsConfigured;
    Vector3 _panMin;
    Vector3 _panMax;

    bool _isDragging;
    Vector3 _lastMousePos;

    Vector3 CurrentOffset => followOffset * _zoomScale;

    void Awake()
    {
        transform.rotation = Quaternion.Euler(fixedEulerAngles);

        if (target != null)
            _focusPoint = target.position;
    }

    /// <summary>Compute the pan clamp box from the map's grid shape. Call after (re)generating the map.</summary>
    public void ConfigureBounds(int boardWidth, int totalRows, float tileSpacingX, float tileSpacingZ, Vector3 originWorld)
    {
        float halfWidth = Mathf.Max(0f, boardWidth - 1) * 0.5f * tileSpacingX;

        _panMin = originWorld + new Vector3(-halfWidth - boundsMargin, 0f, -boundsMargin);
        _panMax = originWorld + new Vector3(halfWidth + boundsMargin, 0f, Mathf.Max(0, totalRows - 1) * tileSpacingZ + boundsMargin);
        _boundsConfigured = true;
    }

    /// <summary>Instant cut to the follow position - no lerp. Used on scene load / map restore.</summary>
    public void SnapToToken()
    {
        _mode = Mode.Follow;
        _zoomScale = 1f;
        _idleTimer = 0f;

        if (target != null)
            _focusPoint = target.position;

        transform.rotation = Quaternion.Euler(fixedEulerAngles);
        transform.position = _focusPoint + CurrentOffset;
    }

    /// <summary>Called whenever the player picks a node - forces the rig back into follow mode.</summary>
    public void NotifyNodeSelected()
    {
        _mode = Mode.Follow;
        _idleTimer = 0f;
    }

    void LateUpdate()
    {
        HandleFreeLookInput();

        if (_mode == Mode.Follow && target != null)
            _focusPoint = target.position;

        Vector3 desiredPos = _focusPoint + CurrentOffset;
        float lerpT = 1f - Mathf.Exp(-followLerpSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPos, lerpT);
        transform.rotation = Quaternion.Euler(fixedEulerAngles);
    }

    void HandleFreeLookInput()
    {
        bool inputThisFrame = false;

        if (Input.GetMouseButtonDown(1))
        {
            _isDragging = true;
            _lastMousePos = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(1))
        {
            _isDragging = false;
        }

        if (_isDragging && Input.GetMouseButton(1))
        {
            // Raw screen-pixel delta, not Input.GetAxis: GetAxis's "Mouse X/Y" is already a
            // sensitivity-damped per-frame value (see Project Settings > Input Manager), and further
            // multiplying it by Time.deltaTime double-applies frame-time scaling - the combination made
            // panning both far too weak and framerate-dependent. Raw pixel delta gives a direct, honest
            // 1:1 relationship between mouse movement and pan distance via panSpeed alone.
            Vector3 mousePos = Input.mousePosition;
            Vector3 delta = mousePos - _lastMousePos;
            _lastMousePos = mousePos;

            if (Mathf.Abs(delta.x) > 0.0001f || Mathf.Abs(delta.y) > 0.0001f)
            {
                inputThisFrame = true;
                _mode = Mode.FreeLook;

                // Drag-to-pan: content follows the cursor (camera moves opposite the drag direction).
                Vector3 pan = new Vector3(-delta.x, 0f, -delta.y) * panSpeed;
                _focusPoint += pan;
                ClampFocusToBounds();
            }
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            inputThisFrame = true;
            _mode = Mode.FreeLook;
            _zoomScale = Mathf.Clamp(_zoomScale - scroll * zoomSpeed, minZoomScale, maxZoomScale);
        }

        if (inputThisFrame)
        {
            _idleTimer = 0f;
            return;
        }

        if (_mode != Mode.FreeLook)
            return;

        _idleTimer += Time.deltaTime;
        if (_idleTimer >= idleReturnDelay)
            _mode = Mode.Follow;
    }

    void ClampFocusToBounds()
    {
        if (!_boundsConfigured)
            return;

        _focusPoint.x = Mathf.Clamp(_focusPoint.x, _panMin.x, _panMax.x);
        _focusPoint.z = Mathf.Clamp(_focusPoint.z, _panMin.z, _panMax.z);
    }
}
