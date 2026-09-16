using System.Collections;
using UnityEngine;

namespace Chess
{
    public class AttackScreenShake : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] Transform shakeTarget;
        [SerializeField] CameraPhaseZoom cameraPhaseZoom;

        [Header("Shake")]
        [SerializeField, Min(0f)] float shakeDuration = 0.08f;
        [SerializeField, Min(0f)] float shakeMagnitude = 0.08f;

        Coroutine _shakeRoutine;

        void Awake()
        {
            if (shakeTarget == null)
                shakeTarget = cameraPhaseZoom != null ? cameraPhaseZoom.ShakeTarget : transform;

            if (shakeTarget == null)
                shakeTarget = transform;

            if (cameraPhaseZoom == null)
                cameraPhaseZoom = GetComponent<CameraPhaseZoom>();
        }

        void OnEnable()
        {
            GameEvents.OnAttackResolved += HandleAttackResolved;
        }

        void OnDisable()
        {
            GameEvents.OnAttackResolved -= HandleAttackResolved;
        }

        void HandleAttackResolved(AttackReport r)
        {
            if (!JuiceSettings.ScreenShakeEnabled)
                return;

            if (cameraPhaseZoom != null && cameraPhaseZoom.IsLifeLossSequencePlaying)
                return;

            if (r.damageToDefender <= 0 && r.damageToAttacker <= 0)
                return;

            if (shakeTarget == null)
                return;

            if (_shakeRoutine != null)
                StopCoroutine(_shakeRoutine);

            _shakeRoutine = StartCoroutine(ShakeRoutine());
        }

        IEnumerator ShakeRoutine()
        {
            Vector3 baseLocalPos = shakeTarget.localPosition;
            float elapsed = 0f;

            while (elapsed < shakeDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                float falloff = 1f - Mathf.Clamp01(elapsed / shakeDuration);
                Vector3 offset = Random.insideUnitSphere * shakeMagnitude * falloff;
                offset.z = 0f;

                shakeTarget.localPosition = baseLocalPos + offset;

                yield return null;
            }

            shakeTarget.localPosition = baseLocalPos;
            _shakeRoutine = null;
        }
    }
}
