using System;
using System.Collections;
using System.Collections.Generic;
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

        Coroutine _routine;
        Vector3 _lastTargetWorld;

        readonly List<SpriteRenderer> _spriteRenderers = new();
        readonly List<Color> _originalColors = new();

        public bool IsPlaying => _routine != null;

        void Awake()
        {
            CacheSpriteRenderers();
        }
        
        void OnEnable()
        {
            RestoreFullAlpha();
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
            _routine = StartCoroutine(Co_PlayBoardSlide(worldFrom, worldTo, tileSize));
        }

        public void PlayAttackBump(Vector3 attackerWorld, Vector3 defenderWorld, Action<Vector3> onImpact = null)
        {
            StopCurrentRoutine();
            RestoreFullAlpha();
            _lastTargetWorld = attackerWorld;
            _routine = StartCoroutine(Co_PlayAttackBump(attackerWorld, defenderWorld, onImpact));
        }

        public void PlayAttackKillAdvance(Vector3 attackerWorld, Vector3 defenderWorld, Vector3 finalWorld, Action<Vector3> onImpact = null)
        {
            StopCurrentRoutine();
            RestoreFullAlpha();
            _lastTargetWorld = finalWorld;
            _routine = StartCoroutine(Co_PlayAttackKillAdvance(attackerWorld, defenderWorld, finalWorld, onImpact));
        }

        public void PlayHitRecoil(Vector3 defenderWorld, Vector3 recoilDirection, bool returnToOrigin)
        {
            StopCurrentRoutine();
            RestoreFullAlpha();
            _lastTargetWorld = defenderWorld;
            _routine = StartCoroutine(Co_PlayHitRecoil(defenderWorld, recoilDirection, returnToOrigin));
        }

        public void PlayDeathRecoilAndDissolve(Vector3 defenderWorld, Vector3 recoilDirection, Action onFinished)
        {
            StopCurrentRoutine();
            RestoreFullAlpha();
            _lastTargetWorld = defenderWorld;
            _routine = StartCoroutine(Co_PlayDeathRecoilAndDissolve(defenderWorld, recoilDirection, onFinished));
        }

        IEnumerator Co_PlayBoardSlide(Vector3 worldFrom, Vector3 worldTo, float tileSize)
        {
            transform.position = worldFrom;

            float distanceInTiles =
                Mathf.Max(0.001f, Vector3.Distance(worldFrom, worldTo) / Mathf.Max(0.001f, tileSize));

            float duration = distanceInTiles / Mathf.Max(0.01f, moveSpeedTilesPerSecond);

            if (duration <= 0f)
            {
                transform.position = worldTo;
                _routine = null;
                yield break;
            }

            yield return AnimateWorldPosition(worldFrom, worldTo, duration, moveEase);

            transform.position = worldTo;
            _routine = null;
        }

        IEnumerator Co_PlayAttackBump(Vector3 attackerWorld, Vector3 defenderWorld, Action<Vector3> onImpact)
        {
            Vector3 dir = defenderWorld - attackerWorld;
            dir.y = 0f;

            if (dir.sqrMagnitude <= 0.0001f)
            {
                transform.position = attackerWorld;
                _routine = null;
                yield break;
            }

            dir.Normalize();

            Vector3 hitPoint = attackerWorld + dir * attackLungeDistance;

            transform.position = attackerWorld;

            yield return AnimateWorldPosition(attackerWorld, hitPoint, lungeDuration, attackEase);

            onImpact?.Invoke(hitPoint);
            yield return new WaitForSeconds(collidePause);

            yield return AnimateWorldPosition(hitPoint, attackerWorld, returnDuration, attackEase);

            transform.position = attackerWorld;
            _routine = null;
        }

        IEnumerator Co_PlayAttackKillAdvance(Vector3 attackerWorld, Vector3 defenderWorld, Vector3 finalWorld, Action<Vector3> onImpact)
        {
            Vector3 dir = defenderWorld - attackerWorld;
            dir.y = 0f;

            if (dir.sqrMagnitude <= 0.0001f)
            {
                transform.position = finalWorld;
                _routine = null;
                yield break;
            }

            dir.Normalize();

            Vector3 hitPoint = attackerWorld + dir * attackLungeDistance;

            transform.position = attackerWorld;

            yield return AnimateWorldPosition(attackerWorld, hitPoint, lungeDuration, attackEase);

            onImpact?.Invoke(hitPoint);
            yield return new WaitForSeconds(collidePause);

            yield return AnimateWorldPosition(hitPoint, finalWorld, killAdvanceDuration, attackEase);

            transform.position = finalWorld;
            _routine = null;
        }

        IEnumerator Co_PlayHitRecoil(Vector3 defenderWorld, Vector3 recoilDirection, bool returnToOrigin)
        {
            recoilDirection.y = 0f;

            if (recoilDirection.sqrMagnitude <= 0.0001f)
            {
                transform.position = defenderWorld;
                _routine = null;
                yield break;
            }

            recoilDirection.Normalize();

            Vector3 recoilTarget = defenderWorld + recoilDirection * hitRecoilDistance;

            transform.position = defenderWorld;

            yield return AnimateWorldPosition(defenderWorld, recoilTarget, hitRecoilOutDuration, hitRecoilEase);

            if (hitRecoilPause > 0f)
                yield return new WaitForSeconds(hitRecoilPause);

            if (returnToOrigin)
            {
                yield return AnimateWorldPosition(recoilTarget, defenderWorld, hitRecoilReturnDuration, hitRecoilEase);
                transform.position = defenderWorld;
            }
            else
            {
                transform.position = recoilTarget;
            }

            _routine = null;
        }

        IEnumerator Co_PlayDeathRecoilAndDissolve(Vector3 defenderWorld, Vector3 recoilDirection, Action onFinished)
        {
            recoilDirection.y = 0f;

            if (recoilDirection.sqrMagnitude <= 0.0001f)
                recoilDirection = Vector3.back;
            else
                recoilDirection.Normalize();

            Vector3 recoilTarget = defenderWorld + recoilDirection * hitRecoilDistance;

            transform.position = defenderWorld;

            yield return AnimateWorldPosition(defenderWorld, recoilTarget, hitRecoilOutDuration, hitRecoilEase);

            if (deathHoldDuration > 0f)
                yield return new WaitForSeconds(deathHoldDuration);

            yield return FadeToAlpha(0f, deathFadeDuration);

            _routine = null;
            onFinished?.Invoke();
        }

        IEnumerator FadeToAlpha(float targetAlpha, float duration)
        {
            if (_spriteRenderers.Count == 0)
                CacheSpriteRenderers();

            float t = 0f;

            var startColors = new List<Color>(_spriteRenderers.Count);
            for (int i = 0; i < _spriteRenderers.Count; i++)
                startColors.Add(_spriteRenderers[i] != null ? _spriteRenderers[i].color : Color.white);

            while (t < duration)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / Mathf.Max(0.0001f, duration));

                for (int i = 0; i < _spriteRenderers.Count; i++)
                {
                    var sr = _spriteRenderers[i];
                    if (sr == null) continue;

                    Color c = startColors[i];
                    c.a = Mathf.Lerp(startColors[i].a, targetAlpha, u);
                    sr.color = c;
                }

                yield return null;
            }

            for (int i = 0; i < _spriteRenderers.Count; i++)
            {
                var sr = _spriteRenderers[i];
                if (sr == null) continue;

                Color c = sr.color;
                c.a = targetAlpha;
                sr.color = c;
            }
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

        IEnumerator AnimateWorldPosition(Vector3 from, Vector3 to, float duration, AnimationCurve curve)
        {
            if (duration <= 0f)
            {
                transform.position = to;
                yield break;
            }

            float t = 0f;

            while (t < duration)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / duration);
                float eased = curve != null ? curve.Evaluate(u) : u;
                transform.position = Vector3.LerpUnclamped(from, to, eased);
                yield return null;
            }

            transform.position = to;
        }

        void StopCurrentRoutine()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            if (_lastTargetWorld != default)
                transform.position = _lastTargetWorld;
        }
    }
}