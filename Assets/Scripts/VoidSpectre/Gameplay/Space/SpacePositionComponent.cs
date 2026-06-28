using System;
using VoidSpectre.Core.Component;
using VoidSpectre.Core.Math;

namespace VoidSpectre.Gameplay.Space
{
    [Serializable]
    public sealed class SpacePositionComponent : BasicGenericTrackableComponent<Float3>
    {
        public SpacePositionComponent() { }
        public SpacePositionComponent(Float3 position) : base(position) { }
    }
}
