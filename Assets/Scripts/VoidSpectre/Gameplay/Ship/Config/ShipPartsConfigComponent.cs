using System;
using System.Collections.Generic;
using Sirenix.Serialization;
using VoidSpectre.Core.Component;
using VoidSpectre.Core.Config;

namespace VoidSpectre.Gameplay.Ship.Config
{
    [Serializable]
    public sealed class ShipPartsConfigComponent : TrackableComponentBase, IConfigComponent
    {
        [OdinSerialize] public List<ShipPartPlacement> Parts = new();
    }
}
