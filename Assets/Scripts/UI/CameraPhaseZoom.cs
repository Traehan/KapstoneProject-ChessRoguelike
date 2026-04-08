using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Chess
{
    public class CameraPhaseZoom : MonoBehaviour
    {
        [Header("Camera Positions")]
        [SerializeField] private Vector3 zoomOutPosition = new Vector3(3.94f, 42.7f, -65.5f); // spell
        [SerializeField] private Vector3 zoomInPosition  = new Vector3(3.94f, 47.13f, -50.99f); // player turn

        [Header("Rotation")]
        [SerializeField] private Vector3 zoomOutRotation = new Vector3(37.912f, 0f, 0f);
        [SerializeField] private Vector3 zoomInRotation  = new Vector3(48.412f, 0f, 0f);

        [Header("Speed")]
        [SerializeField] private float zoomSpeed = 3f;

        [Header("Life Loss Feedback - Camera Shake")]
        [SerializeField] private Transform shakeTarget;
        [SerializeField] private float lifeLossShakeDuration = 0.3f;
        [SerializeField] private float lifeLossShakeMagnitude = 0.45f;
        [SerializeField] private float postShakeZoomOutDelay = 0.05f;

        [Header("Life Loss Feedback - Red Border")]
        [SerializeField] private Image dangerBorderImage;
        [SerializeField] private float borderFlashDuration = 0.4f;
        [SerializeField, Range(0f, 1f)] private float borderMaxAlpha = 0.8f;

        private Coroutine moveRoutine;
        private Coroutine shakeRoutine;
        private Coroutine borderRoutine;
        private Coroutine lifeLossRoutine;

        private bool isLifeLossSequencePlaying;

        private void Awake()
        {
            if (shakeTarget == null)
                shakeTarget = transform;
        }

        private void OnEnable()
        {
            GameEvents.OnPhaseChanged += HandlePhaseChanged;
            GameEvents.OnPlayerLifeLost += HandlePlayerLifeLost;
        }

        private void OnDisable()
        {
            GameEvents.OnPhaseChanged -= HandlePhaseChanged;
            GameEvents.OnPlayerLifeLost -= HandlePlayerLifeLost;
        }

        private void Start()
        {
            if (dangerBorderImage != null)
            {
                Color c = dangerBorderImage.color;
                c.a = 0f;
                dangerBorderImage.color = c;
            }
        }

        private void HandlePhaseChanged(TurnPhase newPhase)
        {
            if (isLifeLossSequencePlaying)
                return;

            switch (newPhase)
            {
                case TurnPhase.PlayerTurn:
                    StartMove(zoomInPosition, zoomInRotation);
                    break;

                case TurnPhase.SpellPhase:
                    StartMove(zoomOutPosition, zoomOutRotation);
                    break;
            }
        }

        private void HandlePlayerLifeLost(int remainingLives)
        {
            if (lifeLossRoutine != null)
                StopCoroutine(lifeLossRoutine);

            lifeLossRoutine = StartCoroutine(LifeLossSequence());
        }

        private IEnumerator LifeLossSequence()
        {
            isLifeLossSequencePlaying = true;

            if (moveRoutine != null)
            {
                StopCoroutine(moveRoutine);
                moveRoutine = null;
            }

            // Hold camera at the zoomed-in combat position first.
            transform.position = zoomInPosition;
            transform.rotation = Quaternion.Euler(zoomInRotation);

            if (shakeRoutine != null)
                StopCoroutine(shakeRoutine);

            shakeRoutine = StartCoroutine(ShakeRoutine());

            if (borderRoutine != null)
                StopCoroutine(borderRoutine);

            borderRoutine = StartCoroutine(BorderFlashRoutine());

            yield return shakeRoutine;

            if (postShakeZoomOutDelay > 0f)
                yield return new WaitForSeconds(postShakeZoomOutDelay);

            isLifeLossSequencePlaying = false;
            StartMove(zoomOutPosition, zoomOutRotation);
            lifeLossRoutine = null;
        }

        private void StartMove(Vector3 targetPos, Vector3 targetRotEuler)
        {
            if (moveRoutine != null)
                StopCoroutine(moveRoutine);

            moveRoutine = StartCoroutine(MoveRoutine(targetPos, Quaternion.Euler(targetRotEuler)));
        }

        private IEnumerator MoveRoutine(Vector3 targetPos, Quaternion targetRot)
        {
            while (Vector3.Distance(transform.position, targetPos) > 0.01f)
            {
                transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * zoomSpeed);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * zoomSpeed);
                yield return null;
            }

            transform.position = targetPos;
            transform.rotation = targetRot;
            moveRoutine = null;
        }

        private IEnumerator ShakeRoutine()
        {
            float elapsed = 0f;

            // Capture the CURRENT pose at shake start, not the startup pose.
            Vector3 baseLocalPos = shakeTarget.localPosition;
            Quaternion baseLocalRot = shakeTarget.localRotation;

            while (elapsed < lifeLossShakeDuration)
            {
                elapsed += Time.deltaTime;

                Vector3 offset = Random.insideUnitSphere * lifeLossShakeMagnitude;
                offset.z = 0f;

                shakeTarget.localPosition = baseLocalPos + offset;
                shakeTarget.localRotation = baseLocalRot * Quaternion.Euler(
                    Random.Range(-1.5f, 1.5f),
                    Random.Range(-1.5f, 1.5f),
                    Random.Range(-1.5f, 1.5f)
                );

                yield return null;
            }

            shakeTarget.localPosition = baseLocalPos;
            shakeTarget.localRotation = baseLocalRot;
            shakeRoutine = null;
        }

        private IEnumerator BorderFlashRoutine()
        {
            if (dangerBorderImage == null)
                yield break;

            Color c = dangerBorderImage.color;

            float half = borderFlashDuration * 0.5f;
            float t = 0f;

            while (t < half)
            {
                t += Time.deltaTime;
                c.a = Mathf.Lerp(0f, borderMaxAlpha, t / half);
                dangerBorderImage.color = c;
                yield return null;
            }

            t = 0f;

            while (t < half)
            {
                t += Time.deltaTime;
                c.a = Mathf.Lerp(borderMaxAlpha, 0f, t / half);
                dangerBorderImage.color = c;
                yield return null;
            }

            c.a = 0f;
            dangerBorderImage.color = c;
            borderRoutine = null;
        }
    }
}