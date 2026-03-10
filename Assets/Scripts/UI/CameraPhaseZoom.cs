using System.Collections;
using UnityEngine;

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

        private Coroutine moveRoutine;

        private void OnEnable()
        {
            GameEvents.OnPhaseChanged += HandlePhaseChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnPhaseChanged -= HandlePhaseChanged;
        }

        private void HandlePhaseChanged(TurnPhase newPhase)
        {
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
        }
    }
}