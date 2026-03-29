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

        public void AddStacks(StatusId id, int amount, Piece source = null)
        {
            if (amount <= 0) return;

            int previous = GetStacks(id);
            int next = Mathf.Max(0, previous + amount);
            _stacks[id] = next;

            FireChanged();

            GameEvents.OnStatusApplied?.Invoke(new StatusChangeReport
            {
                piece = _piece,
                statusId = id,
                amountChanged = amount,
                previousStacks = previous,
                newStacks = next,
                source = source
            });
        }

        public void SetStacks(StatusId id, int value, Piece source = null)
        {
            value = Mathf.Max(0, value);

            int previous = GetStacks(id);
            if (previous == value)
                return;

            if (value == 0) _stacks.Remove(id);
            else _stacks[id] = value;

            FireChanged();

            int delta = value - previous;

            if (delta > 0)
            {
                GameEvents.OnStatusApplied?.Invoke(new StatusChangeReport
                {
                    piece = _piece,
                    statusId = id,
                    amountChanged = delta,
                    previousStacks = previous,
                    newStacks = value,
                    source = source
                });
            }
            else
            {
                GameEvents.OnStatusRemoved?.Invoke(new StatusChangeReport
                {
                    piece = _piece,
                    statusId = id,
                    amountChanged = -delta,
                    previousStacks = previous,
                    newStacks = value,
                    source = source
                });
            }
        }

        public void Clear(StatusId id, Piece source = null)
        {
            int previous = GetStacks(id);
            if (previous <= 0) return;

            _stacks.Remove(id);
            FireChanged();

            GameEvents.OnStatusRemoved?.Invoke(new StatusChangeReport
            {
                piece = _piece,
                statusId = id,
                amountChanged = previous,
                previousStacks = previous,
                newStacks = 0,
                source = source
            });
        }

        public List<StatusEntry> GetAll()
        {
            var list = new List<StatusEntry>(_stacks.Count);
            foreach (var kv in _stacks)
                list.Add(new StatusEntry { id = kv.Key, stacks = kv.Value });
            return list;
        }

        public List<StatusEntry> CaptureSnapshot()
        {
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