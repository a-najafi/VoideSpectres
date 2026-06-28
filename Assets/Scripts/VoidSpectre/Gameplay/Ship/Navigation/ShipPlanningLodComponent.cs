using System;
using Sirenix.Serialization;
using VoidSpectre.Core.Component;

namespace VoidSpectre.Gameplay.Ship.Navigation
{
    public enum ShipPlanningLodTier
    {
        Dormant = 0,
        Background = 1,
        Visible = 2,
        Hero = 3,
    }

    [Serializable]
    public sealed class ShipPlanningLodComponent : TrackableComponentBase
    {
        [OdinSerialize] private ShipPlanningLodTier _tier = ShipPlanningLodTier.Hero;

        public ShipPlanningLodTier Tier
        {
            get => _tier;
            set => SetField(ref _tier, value);
        }
    }
}
