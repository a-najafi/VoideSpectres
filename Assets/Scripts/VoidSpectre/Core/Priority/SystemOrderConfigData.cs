using System;
using System.Collections.Generic;
using Sirenix.Serialization;

namespace VoidSpectre.Core.Priority
{
    [Serializable]
    public sealed class SystemOrderConfigData
    {
        [Serializable]
        public class Entry
        {
            [OdinSerialize] public string systemTypeName;
            [OdinSerialize] public string triggerTypeName;
            [OdinSerialize] public int priority;
        }

        [OdinSerialize] public List<Entry> entries = new();
    }
}
