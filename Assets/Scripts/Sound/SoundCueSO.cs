using UnityEngine;
using UnityEngine.Audio;

namespace Chess
{
    [CreateAssetMenu(menuName = "Chess/Audio/Sound Cue", fileName = "SFX_")]
    public class SoundCueSO : ScriptableObject
    {
        [Header("Clips")]
        public AudioClip[] clips;

        [Header("Mix")]
        [Range(0f, 1f)] public float volumeMin = 1f;
        [Range(0f, 1f)] public float volumeMax = 1f;
        [Range(-3f, 3f)] public float pitchMin = 1f;
        [Range(-3f, 3f)] public float pitchMax = 1f;

        [Header("Playback")]
        public bool spatial = true;
        [Range(0f, 1f)] public float spatialBlend = 1f;
        public float minDistance = 1f;
        public float maxDistance = 15f;
        public AudioMixerGroup outputGroup;

        public AudioClip GetRandomClip()
        {
            if (clips == null || clips.Length == 0)
                return null;

            int i = Random.Range(0, clips.Length);
            return clips[i];
        }

        public void ApplyToSource(AudioSource source, AudioClip clip)
        {
            if (source == null || clip == null)
                return;

            source.clip = clip;
            source.outputAudioMixerGroup = outputGroup;
            source.volume = Random.Range(volumeMin, volumeMax);
            source.pitch = Random.Range(pitchMin, pitchMax);
            source.spatialBlend = spatial ? spatialBlend : 0f;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.loop = false;
            source.playOnAwake = false;
        }
    }
}