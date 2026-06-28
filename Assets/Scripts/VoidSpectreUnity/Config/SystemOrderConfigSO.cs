using System;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using VoidSpectre.Core.Priority;

namespace VoidSpectreUnity.Config
{
    [CreateAssetMenu(menuName = "VoidSpectre/System Order Config")]
    public sealed class SystemOrderConfigSO : SerializedScriptableObject
    {
        [OdinSerialize] public SystemOrderConfigData Data = new();

        public SystemOrderConfigData ToData() => Data;
    }
}
