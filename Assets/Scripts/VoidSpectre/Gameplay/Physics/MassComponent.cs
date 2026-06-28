using System;
using VoidSpectre.Core.Component;

namespace VoidSpectre.Gameplay.Physics
{
    [Serializable]
    public sealed class MassComponent : BasicGenericTrackableComponent<float>
    {
        public MassComponent() { }
        public MassComponent(float kilograms) : base(kilograms) { }
    }
}
