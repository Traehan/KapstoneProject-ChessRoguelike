// Assets/Enemies/EncounterSetup/Map3D/MapClanToken.cs
//
// Placeholder world-space marker for the player's position on the 3D map. Not the clan's Queen piece
// model (explicit design decision) - swappable for real art later. Mirrors the project's DOTween usage
// convention (see Chess.PieceMotionController): SnapTo for instant placement (scene load/resume, no walk
// animation), MoveTo for an animated slide when the player actually picks a new node.

using DG.Tweening;
using UnityEngine;

public class MapClanToken : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0.01f)] float moveDuration = 0.45f;
    [SerializeField] AnimationCurve moveEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Placeholder Look")]
    [SerializeField] Renderer markerRenderer;
    [SerializeField] Color tokenColor = new Color(0.2f, 0.85f, 1f, 1f);

    Tween _activeTween;

    public bool IsMoving => _activeTween != null && _activeTween.IsActive();

    void Awake()
    {
        if (markerRenderer == null)
            markerRenderer = GetComponentInChildren<Renderer>();

        ApplyTokenColor();
    }

    void ApplyTokenColor()
    {
        if (markerRenderer == null)
            return;

        var mpb = new MaterialPropertyBlock();
        markerRenderer.GetPropertyBlock(mpb);
        mpb.SetColor("_Color", tokenColor);
        mpb.SetColor("_BaseColor", tokenColor);
        markerRenderer.SetPropertyBlock(mpb);
    }

    /// <summary>Instantly place the token (scene load/resume - no walk animation).</summary>
    public void SnapTo(Vector3 worldPos)
    {
        KillActiveTween();
        transform.position = worldPos;
    }

    /// <summary>Animate a slide to the new node's world position (an actual node pick).</summary>
    public void MoveTo(Vector3 worldPos)
    {
        KillActiveTween();

        _activeTween = transform.DOMove(worldPos, moveDuration)
            .SetEase(moveEase)
            .OnComplete(() => _activeTween = null);
    }

    void KillActiveTween()
    {
        if (_activeTween != null && _activeTween.IsActive())
            _activeTween.Kill();

        _activeTween = null;
    }

    void OnDestroy()
    {
        KillActiveTween();
    }
}
