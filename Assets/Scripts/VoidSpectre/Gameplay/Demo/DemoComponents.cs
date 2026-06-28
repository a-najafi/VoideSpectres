using System;
using VoidSpectre.Core.Component;

namespace VoidSpectre.Gameplay.Demo
{
    [Serializable]
    public sealed class DemoCrewTagComponent : TagComponent { }

    [Serializable]
    public sealed class DemoShipHullTagComponent : TagComponent { }

    [Serializable]
    public sealed class DemoInteriorPositionComponent : BasicGenericTrackableComponent<float>
    {
        public DemoInteriorPositionComponent() { }
        public DemoInteriorPositionComponent(float value) : base(value) { }
    }

    [Serializable]
    public sealed class DemoInteriorLayoutComponent : BasicGenericTrackableComponent<string>
    {
        public DemoInteriorLayoutComponent() { }
        public DemoInteriorLayoutComponent(string value) : base(value) { }
    }
}
