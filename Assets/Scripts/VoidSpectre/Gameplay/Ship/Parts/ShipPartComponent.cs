using System;
using Sirenix.Serialization;
using VoidSpectre.Core;
using VoidSpectre.Core.Component;

namespace VoidSpectre.Gameplay.Ship.Parts
{
    [Serializable]
    public sealed class ShipPartComponent : TrackableComponentBase
    {
        [OdinSerialize] public ComponentStore.EntityId ParentShip;
    }
}
