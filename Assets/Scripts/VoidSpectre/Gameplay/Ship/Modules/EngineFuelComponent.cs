using System;
using Sirenix.Serialization;
using VoidSpectre.Core.Component;
using VoidSpectre.Core.Math;

namespace VoidSpectre.Gameplay.Ship.Modules
{
    [Serializable]
    public sealed class EngineFuelComponent : TrackableComponentBase
    {
        [OdinSerialize] public float MaxFuelLiters;
        [OdinSerialize] public float CurrentFuelLiters;

        public bool HasFuel => CurrentFuelLiters > 0f;

        public float Consume(float liters)
        {
            if (liters <= 0f) return 0f;
            var consumed = VsMath.Min(CurrentFuelLiters, liters);
            CurrentFuelLiters -= consumed;
            BumpVersion();
            return consumed;
        }
    }
}
