using System;
using System.Collections;
using UnityEngine;

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

        Coroutine _routine;
        Vector3 _lastTargetWorld;

        public bool IsPlaying => _routine != null;

        public void SnapToWorld(Vector3 worldPos)
        {
            StopCurrentRoutine();
            transform.position = worldPos;
            _lastTargetWorld = worldPos;
        }

        public void PlayBoardSlide(Vector3 worldFrom, Vector3 worldTo, float tileSize = 1f)
        {
            StopCurrentRoutine();
            _lastTargetWorld = worldTo;
            _routine = StartCoroutine(Co_PlayBoardSlide(worldFrom, worldTo, tileSize));
        }

        public void PlayAttackBump(Vector3 attackerWorld, Vector3 defenderWorld, Action<Vector3> onImpact = null)
        {
            StopCurrentRoutine();
            _lastTargetWorld = attackerWorld;
            _routine = StartCoroutine(Co_PlayAttackBump(attackerWorld, defenderWorld, onImpact));
        }

        public void PlayAttackKillAdvance(Vector3 attackerWorld, Vector3 defenderWorld, Vector3 finalWorld, Action<Vector3> onImpact = null)
        {
            StopCurrentRoutine();
            _lastTargetWorld = finalWorld;
            _routine = StartCoroutine(Co_PlayAttackKillAdvance(attackerWorld, defenderWorld, finalWorld, onImpact));
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