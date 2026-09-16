using System.Collections;
using UnityEngine;

namespace Chess
{
    [DisallowMultipleComponent]
    public class HitStopController : MonoBehaviour
    {
        static HitStopController _instance;

        public static HitStopController Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("HitStopController");
                    _instance = go.AddComponent<HitStopController>();
                }

                return _instance;
            }
        }

        [Header("Hit Stop")]
        [SerializeField, Range(0f, 1f)] float freezeTimeScale = 0.02f;
        [SerializeField, Min(0f)] float defaultFreezeDuration = 0.05f;

        Coroutine _routine;
        float _savedTimeScale = 1f;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void RequestHitStop(float? duration = null)
        {
            if (!JuiceSettings.HitStopEnabled || JuiceSettings.SpeedPreset == GameSpeedPreset.Instant)
                return;

            float freezeDuration = duration ?? defaultFreezeDuration;
            if (freezeDuration <= 0f)
                return;

            if (_routine != null)
                StopCoroutine(_routine);
            else
                _savedTimeScale = Time.timeScale;

            _routine = StartCoroutine(HitStopRoutine(freezeDuration));
        }

        IEnumerator HitStopRoutine(float duration)
        {
            Time.timeScale = freezeTimeScale;

            yield return new WaitForSecondsRealtime(duration);

            Time.timeScale = _savedTimeScale;
            _routine = null;
        }
    }
}
