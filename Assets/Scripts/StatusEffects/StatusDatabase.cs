using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    [CreateAssetMenu(menuName = "Chess/Status Database", fileName = "StatusDatabase")]
    public sealed class StatusDatabase : ScriptableObject
    {
        public List<StatusDefinition> definitions = new();

        Dictionary<StatusId, StatusDefinition> _map;

        public StatusDefinition Get(StatusId id)
        {
            if (_map == null)
            {
                _map = new Dictionary<StatusId, StatusDefinition>();
                foreach (var d in definitions)
                    if (d != null) _map[d.id] = d;
            }

            _map.TryGetValue(id, out var def);
            return def;
        }
    }
}