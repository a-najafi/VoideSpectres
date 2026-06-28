using System;
using System.Collections.Generic;

namespace VoidSpectre.Core.Priority
{
    public sealed class SystemOrderProvider
    {
        private readonly Dictionary<(string systemType, string triggerType), int> _priorities;

        public SystemOrderProvider(SystemOrderConfigData config)
        {
            _priorities = new Dictionary<(string, string), int>();

            if (config?.entries != null)
            {
                foreach (var entry in config.entries)
                    _priorities[(entry.systemTypeName, entry.triggerTypeName)] = entry.priority;
            }
        }

        public int GetPriority(Type systemType, Type triggerType)
        {
            var key = (systemType.FullName, triggerType?.FullName ?? "CoreUpdate");
            return _priorities.TryGetValue(key, out var prio) ? prio : 0;
        }
    }
}
