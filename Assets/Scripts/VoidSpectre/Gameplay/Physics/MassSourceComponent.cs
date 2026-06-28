using System;
using VoidSpectre.Core.Component;

namespace VoidSpectre.Gameplay.Physics
{
    [Serializable]
    public sealed class MassSourceComponent : BasicGenericTrackableComponent<float>
    {
        public MassSourceComponent() { }
        public MassSourceComponent(float kilograms) : base(kilograms) { }
    }
}
