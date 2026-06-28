using System;
using Sirenix.Serialization;
using UnityEngine;
using VoidSpectre.Core.Component;

namespace VoidSpectreUnity.View
{
    [Serializable]
    public sealed class EntityVisualComponent : TrackableComponentBase
    {
        [OdinSerialize] public GameObject Prefab;
    }
}
