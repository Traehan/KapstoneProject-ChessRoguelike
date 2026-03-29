using System;
using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    [CreateAssetMenu(menuName = "Chess/Audio/Sound Profile", fileName = "SoundProfile")]
    public class SoundProfileSO : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public SoundEventId eventId;
            public SoundCueSO cue;
        }

        [SerializeField] List<Entry> entries = new();

        Dictionary<SoundEventId, SoundCueSO> _map;

        public SoundCueSO GetCue(SoundEventId eventId)
        {
            if (_map == null)
                RebuildMap();

            return _map.TryGetValue(eventId, out var cue) ? cue : null;
        }

        void OnValidate()
        {
            _map = null;
        }

        void RebuildMap()
        {
            _map = new Dictionary<SoundEventId, SoundCueSO>();

            if (entries == null)
                return;

            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e.eventId == SoundEventId.None || e.cue == null)
                    continue;

                _map[e.eventId] = e.cue;
            }
        }
    }
}