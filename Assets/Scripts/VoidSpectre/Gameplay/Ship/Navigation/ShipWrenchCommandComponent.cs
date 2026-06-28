using System;
using Sirenix.Serialization;
using VoidSpectre.Core.Component;
using VoidSpectre.Core.Math;

namespace VoidSpectre.Gameplay.Ship.Navigation
{
    /// <summary>
    /// Desired body-frame wrench in physical units (Newtons / Newton-metres).
    /// Produced by plan execution and consumed by control allocation.
    /// </summary>
    [Serializable]
    public sealed class ShipWrenchCommandComponent : TrackableComponentBase
    {
        [OdinSerialize] private Float3 _desiredForceBody;
        [OdinSerialize] private Float3 _desiredTorqueBody;

        public Float3 DesiredForceBody
        {
            get => _desiredForceBody;
            set => SetField(ref _desiredForceBody, value);
        }

        public Float3 DesiredTorqueBody
        {
            get => _desiredTorqueBody;
            set => SetField(ref _desiredTorqueBody, value);
        }

        public void Set(Float3 forceBody, Float3 torqueBody)
        {
            _desiredForceBody = forceBody;
            _desiredTorqueBody = torqueBody;
            BumpVersion();
        }

        public void Clear() => Set(Float3.Zero, Float3.Zero);
    }
}
