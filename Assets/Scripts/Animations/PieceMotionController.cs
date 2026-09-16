using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;

namespace Chess
{
    [DisallowMultipleComponent]
    public class PieceMotionController : MonoBehaviour
    {
        [Header("Board Slide")]
        [SerializeField, Min(0.01f)] float moveSpeedTilesPerSecond = 7f;
        [SerializeField] AnimationCurve moveEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Attack Presentation")]
        [SerializeField, Min(0f)] float attackLungeDistance = 0.35f;
        [SerializeField, Min(0.01f)] float lungeDuration = 0.08f;
        [SerializeField, Min(0f)] float collidePause = 0.05f;
        [SerializeField, Min(0.01f)] float returnDuration = 0.10f;
        [SerializeField, Min(0.01f)] float killAdvanceDuration = 0.12f;
        [SerializeField] AnimationCurve attackEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Hit Recoil")]
        [SerializeField, Min(0f)] float hitRecoilDistance = 0.20f;
        [SerializeField, Min(0.01f)] float hitRecoilOutDuration = 0.06f;
        [SerializeField, Min(0f)] float hitRecoilPause = 0.03f;
        [SerializeField, Min(0.01f)] float hitRecoilReturnDuration = 0.09f;
        [SerializeField] AnimationCurve hitRecoilEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Death Dissolve")]
        [SerializeField, Min(0f)] float deathHoldDuration = 0.06f;
        [SerializeField, Min(0.01f)] float deathFadeDuration = 0.18f;

        public static int ActiveAnimationCount { get; private set; }

        Sequence _activeSequence;
        Vector3 _lastTargetWorld;
        bool _gateOpen;

        readonly List<SpriteRenderer> _spriteRenderers = new();
        readonly List<Color> _originalColors = new();

        public bool IsPlaying => _activeSequence != null && _activeSequence.IsActive();

        void Awake()
        {
            CacheSpriteRenderers();
        }

        void OnEnable()
        {
            RestoreFullAlpha();
        }

        void OnDisable()
        {
            EndGate();
        }

        public void ResetVisualState()
        {
            StopCurrentRoutine();
            RestoreFullAlpha();
        }

        void CacheSpriteRenderers()
        {
            _spriteRenderers.Clear();
            _originalColors.Clear();

            GetComponentsInChildren(true, _spriteRenderers);

            for (int i = 0; i < _spriteRenderers.Count; i++)
                _originalColors.Add(_spriteRenderers[i] != null ? _spriteRenderers[i].color : Color.white);
        }

        void BeginGate()
        {
            if (_gateOpen)
                return;

            _gateOpen = true;
            ActiveAnimationCount++;
        }

        void EndGate()
        {
            if (!_gateOpen)
                return;

            _gateOpen = false;
            ActiveAnimationCount = Mathf.Max(0, ActiveAnimationCount - 1);
        }

        // Scaled by JuiceSettings.AnimationDurationMultiplier, read live so a mid-battle
        // speed change takes effect starting with the next animation.
        static float Scaled(float duration) => duration * JuiceSettings.AnimationDurationMultiplier;

        public void SnapToWorld(Vector3 worldPos)
        {
            StopCurrentRoutine();
            RestoreFullAlpha();
            transform.position = worldPos;
            _lastTargetWorld = worldPos;
        }

        public void PlayBoardSlide(Vector3 worldFrom, Vector3 worldTo, float tileSize = 1f)
        {
            StopCurrentRoutine();
            RestoreFullAlpha();
            _lastTargetWorld = worldTo;

            float distanceInTiles =
                Mathf.Max(0.001f, Vector3.Distance(worldFrom, worldTo) / Mathf.Max(0.001f, tileSize));

            float duration = Scaled(distanceInTiles / Mathf.Max(0.01f, moveSpeedTilesPerSecond));

            transform.position = worldFrom;

            if (duration <= 0f)
            {
                transform.position = worldTo;
                return;
            }

            BeginGate();

            _activeSequence = DOTween.Sequence();
            _activeSequence.Append(transform.DOMove(worldTo, duration).SetEase(moveEase));
            _activeSequence.OnComplete(() =>
            {
                transform.position = worldTo;
                _activeSequence = null;
                EndGate();
            });
            _activeSequence.OnKill(EndGate);
        }

        public void PlayAttackBump(Vector3 attackerWorld, Vector3 defenderWorld, Action<Vector3> onImpact = null)
        {
            StopCurrentRoutine();
            RestoreFullAlpha();
            _lastTargetWorld = attackerWorld;

            Vector3 dir = defenderWorld - attackerWorld;
            dir.y = 0f;

            transform.position = attackerWorld;

            if (dir.sqrMagnitude <= 0.0001f)
                return;

            dir.Normalize();

            Vector3 hitPoint = attackerWorld + dir * attackLungeDistance;

            BeginGate();

            _activeSequence = DOTween.Sequence();
            _activeSequence.Append(transform.DOMove(hitPoint, Scaled(lungeDuration)).SetEase(attackEase));
            _activeSequence.AppendCallback(() => onImpact?.Invoke(hitPoint));

            if (collidePause > 0f)
                _activeSequence.AppendInterval(Scaled(collidePause));

            _activeSequence.Append(transform.DOMove(attackerWorld, Scaled(returnDuration)).SetEase(attackEase));
            _activeSequence.OnComplete(() =>
            {
                transform.position = attackerWorld;
                _activeSequence = null;
                EndGate();
            });
            _activeSequence.OnKill(EndGate);
        }

        public void PlayAttackKillAdvance(Vector3 attackerWorld, Vector3 defenderWorld, Vector3 finalWorld, Action<Vector3> onImpact = null)
        {
            StopCurrentRoutine();
            RestoreFullAlpha();
            _lastTargetWorld = finalWorld;

            Vector3 dir = defenderWorld - attackerWorld;
            dir.y = 0f;

            transform.position = attackerWorld;

            if (dir.sqrMagnitude <= 0.0001f)
            {
                transform.position = finalWorld;
                return;
            }

            dir.Normalize();

            Vector3 hitPoint = attackerWorld + dir * attackLungeDistance;

            BeginGate();

            _activeSequence = DOTween.Sequence();
            _activeSequence.Append(transform.DOMove(hitPoint, Scaled(lungeDuration)).SetEase(attackEase));
            _activeSequence.AppendCallback(() => onImpact?.Invoke(hitPoint));

            if (collidePause > 0f)
                _activeSequence.AppendInterval(Scaled(collidePause));

            _activeSequence.Append(transform.DOMove(finalWorld, Scaled(killAdvanceDuration)).SetEase(attackEase));
            _activeSequence.OnComplete(() =>
            {
                transform.position = finalWorld;
                _activeSequence = null;
                EndGate();
            });
            _activeSequence.OnKill(EndGate);
        }

        public void PlayHitRecoil(Vector3 defenderWorld, Vector3 recoilDirection, bool returnToOrigin)
        {
            StopCurrentRoutine();
            RestoreFullAlpha();
            _lastTargetWorld = defenderWorld;

            recoilDirection.y = 0f;

            transform.position = defenderWorld;

            if (recoilDirection.sqrMagnitude <= 0.0001f)
                return;

            recoilDirection.Normalize();

            Vector3 recoilTarget = defenderWorld + recoilDirection * hitRecoilDistance;

            BeginGate();

            _activeSequence = DOTween.Sequence();
            _activeSequence.Append(transform.DOMove(recoilTarget, Scaled(hitRecoilOutDuration)).SetEase(hitRecoilEase));

            if (hitRecoilPause > 0f)
                _activeSequence.AppendInterval(Scaled(hitRecoilPause));

            if (returnToOrigin)
            {
                _activeSequence.Append(transform.DOMove(defenderWorld, Scaled(hitRecoilReturnDuration)).SetEase(hitRecoilEase));
                _activeSequence.OnComplete(() =>
                {
                    transform.position = defenderWorld;
                    _activeSequence = null;
                    EndGate();
                });
            }
            else
            {
                _activeSequence.OnComplete(() =>
                {
                    transform.position = recoilTarget;
                    _lastTargetWorld = recoilTarget;
                    _activeSequence = null;
                    EndGate();
                });
            }

            _activeSequence.OnKill(EndGate);
        }

        public void PlayDeathRecoilAndDissolve(Vector3 defenderWorld, Vector3 recoilDirection, Action onFinished)
        {
            StopCurrentRoutine();
            RestoreFullAlpha();
            _lastTargetWorld = defenderWorld;

            recoilDirection.y = 0f;

            if (recoilDirection.sqrMagnitude <= 0.0001f)
                recoilDirection = Vector3.back;
            else
                recoilDirection.Normalize();

            Vector3 recoilTarget = defenderWorld + recoilDirection * hitRecoilDistance;

            transform.position = defenderWorld;

            if (_spriteRenderers.Count == 0)
                CacheSpriteRenderers();

            BeginGate();

            _activeSequence = DOTween.Sequence();
            _activeSequence.Append(transform.DOMove(recoilTarget, Scaled(hitRecoilOutDuration)).SetEase(hitRecoilEase));

            if (deathHoldDuration > 0f)
                _activeSequence.AppendInterval(Scaled(deathHoldDuration));

            float fadeDuration = Scaled(deathFadeDuration);
            bool firstFade = true;

            for (int i = 0; i < _spriteRenderers.Count; i++)
            {
                var sr = _spriteRenderers[i];
                if (sr == null)
                    continue;

                var fadeTween = sr.DOFade(0f, fadeDuration);

                if (firstFade)
                {
                    _activeSequence.Append(fadeTween);
                    firstFade = false;
                }
                else
                {
                    _activeSequence.Join(fadeTween);
                }
            }

            _activeSequence.OnComplete(() =>
            {
                _activeSequence = null;
                EndGate();
                onFinished?.Invoke();
            });
            _activeSequence.OnKill(EndGate);
        }

        void RestoreFullAlpha()
        {
            if (_spriteRenderers.Count == 0)
                CacheSpriteRenderers();

            for (int i = 0; i < _spriteRenderers.Count; i++)
            {
                var sr = _spriteRenderers[i];
                if (sr == null) continue;

                var baseColor = i < _originalColors.Count ? _originalColors[i] : sr.color;
                sr.color = baseColor;
            }
        }

        void StopCurrentRoutine()
        {
            if (_activeSequence != null && _activeSequence.IsActive())
                _activeSequence.Kill();

            _activeSequence = null;

            // Idempotent: also called from the killed sequence's OnKill callback above.
            EndGate();

            if (_lastTargetWorld != default)
                transform.position = _lastTargetWorld;
        }
    }
}
