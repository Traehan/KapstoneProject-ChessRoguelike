using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    [DisallowMultipleComponent]
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [Header("Defaults")]
        [SerializeField] SoundProfileSO defaultProfile;

        [Header("Pooling")]
        [SerializeField, Min(1)] int initial2DSourceCount = 8;
        [SerializeField, Min(1)] int initial3DSourceCount = 12;

        [Header("Music")]
        [SerializeField] AudioSource musicSource;

        readonly List<AudioSource> _pool2D = new();
        readonly List<AudioSource> _pool3D = new();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            EnsurePool(_pool2D, initial2DSourceCount, false, "SFX_2D");
            EnsurePool(_pool3D, initial3DSourceCount, true, "SFX_3D");

            if (musicSource == null)
            {
                var go = new GameObject("MusicSource");
                go.transform.SetParent(transform, false);
                musicSource = go.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
                musicSource.spatialBlend = 0f;
            }
        }

        public void PlayGlobal(SoundEventId eventId, SoundProfileSO overrideProfile = null)
        {
            var cue = ResolveCue(eventId, overrideProfile);
            PlayCue(cue, null);
        }

        public void PlayAt(SoundEventId eventId, Vector3 worldPos, SoundProfileSO overrideProfile = null)
        {
            var cue = ResolveCue(eventId, overrideProfile);
            PlayCue(cue, worldPos);
        }

        public void PlayCue(SoundCueSO cue, Vector3? worldPos)
        {
            if (cue == null)
                return;

            var clip = cue.GetRandomClip();
            if (clip == null)
                return;

            bool spatial = cue.spatial;
            var source = GetFreeSource(spatial);

            if (worldPos.HasValue)
                source.transform.position = worldPos.Value;

            cue.ApplyToSource(source, clip);
            source.Play();
        }

        public void PlayMusic(SoundCueSO cue)
        {
            if (musicSource == null)
                return;

            if (cue == null)
            {
                musicSource.Stop();
                musicSource.clip = null;
                return;
            }

            var clip = cue.GetRandomClip();
            if (clip == null)
                return;

            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
            musicSource.volume = Random.Range(cue.volumeMin, cue.volumeMax);
            musicSource.pitch = 1f;
            musicSource.outputAudioMixerGroup = cue.outputGroup;
            musicSource.Play();
        }

        public SoundCueSO ResolveCue(SoundEventId eventId, SoundProfileSO overrideProfile = null)
        {
            if (overrideProfile != null)
            {
                var overrideCue = overrideProfile.GetCue(eventId);
                if (overrideCue != null)
                    return overrideCue;
            }

            return defaultProfile != null ? defaultProfile.GetCue(eventId) : null;
        }

        void EnsurePool(List<AudioSource> pool, int count, bool spatial, string baseName)
        {
            while (pool.Count < count)
                pool.Add(CreateSource(baseName + "_" + pool.Count, spatial));
        }

        AudioSource GetFreeSource(bool spatial)
        {
            var pool = spatial ? _pool3D : _pool2D;

            for (int i = 0; i < pool.Count; i++)
            {
                if (!pool[i].isPlaying)
                    return pool[i];
            }

            var extra = CreateSource(spatial ? "SFX_3D_Extra" : "SFX_2D_Extra", spatial);
            pool.Add(extra);
            return extra;
        }

        AudioSource CreateSource(string name, bool spatial)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);

            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = spatial ? 1f : 0f;

            return source;
        }
    }
}