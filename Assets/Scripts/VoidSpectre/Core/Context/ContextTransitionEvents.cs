using System;
using Sirenix.Serialization;
using VoidSpectre.Core;

namespace VoidSpectre.Core.Context
{
    [Serializable]
    public struct EntityExitContextRequested
    {
        [OdinSerialize] public ComponentStore.EntityId Entity;
        [OdinSerialize] public SimulationContextId DestinationContextId;
    }

    [Serializable]
    public struct EntityEnteredContext
    {
        [OdinSerialize] public ComponentStore.EntityId Entity;
        [OdinSerialize] public SimulationContextId SourceContextId;
    }

    [Serializable]
    public struct EntityExitedContext
    {
        [OdinSerialize] public ComponentStore.EntityId Entity;
        [OdinSerialize] public SimulationContextId DestinationContextId;
    }
}
