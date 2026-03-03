using System;
using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    [DisallowMultipleComponent]
    public sealed class StatusController : MonoBehaviour
    {
        [Serializable]
        public struct StatusEntry
        {
            public StatusId id;
            public int stacks;
        }

        readonly Dictionary<StatusId, int> _stacks = new();

        Piece _piece;

        public event Action<StatusController> OnStatusesChanged;

        void Awake()
        {
            _piece = GetComponent<Piece>();
        }

        public int GetStacks(StatusId id)
        {
            return _stacks.TryGetValue(id, out var s) ? s : 0;
        }

        public void AddStacks(StatusId id, int amount)
        {
            if (amount <= 0) return;

            _stacks.TryGetValue(id, out var cur);
            _stacks[id] = Mathf.Max(0, cur + amount);

            FireChanged();
        }

        public void SetStacks(StatusId id, int value)
        {
            value = Mathf.Max(0, value);

            if (value == 0) _stacks.Remove(id);
            else _stacks[id] = value;

            FireChanged();
        }

        public void Clear(StatusId id)
        {
            if (_stacks.Remove(id))
                FireChanged();
        }

        public List<StatusEntry> GetAll()
        {
            var list = new List<StatusEntry>(_stacks.Count);
            foreach (var kv in _stacks)
                list.Add(new StatusEntry { id = kv.Key, stacks = kv.Value });
            return list;
        }

        // ----------------------------
        // Undo support
        // ----------------------------

        public List<StatusEntry> CaptureSnapshot()
        {
            // Copy current stacks into a standalone list
            return GetAll();
        }

        public void RestoreSnapshot(List<StatusEntry> snapshot)
        {
            _stacks.Clear();

            if (snapshot != null)
            {
                for (int i = 0; i < snapshot.Count; i++)
                {
                    var e = snapshot[i];
                    if (e.stacks > 0)
                        _stacks[e.id] = e.stacks;
                }
            }

            FireChanged();
        }

        void FireChanged()
        {
            OnStatusesChanged?.Invoke(this);
            GameEvents.OnPieceStatsChanged?.Invoke(_piece);
        }
    }
}