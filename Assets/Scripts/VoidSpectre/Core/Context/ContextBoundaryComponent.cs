using System;
using Sirenix.Serialization;
using VoidSpectre.Core.Component;

namespace VoidSpectre.Core.Context
{
    [Serializable]
    public sealed class ContextBoundaryComponent : TrackableComponentBase
    {
        [OdinSerialize] public SimulationContextId LinkedContextId;
        [OdinSerialize] public bool IsParentSide;
    }
}
