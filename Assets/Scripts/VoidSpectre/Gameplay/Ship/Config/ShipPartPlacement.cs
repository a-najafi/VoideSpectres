using System;
using Sirenix.Serialization;
using VoidSpectre.Core.Config;
using VoidSpectre.Core.Math;

namespace VoidSpectre.Gameplay.Ship.Config
{
    [Serializable]
    public sealed class ShipPartPlacement
    {
        [OdinSerialize] public IEntityArchetype Archetype;
        [OdinSerialize] public Float3 LocalPosition;
        [OdinSerialize] public FloatQuaternion LocalOrientation = FloatQuaternion.Identity;
    }
}
