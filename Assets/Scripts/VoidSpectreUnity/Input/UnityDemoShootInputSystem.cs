using VoidSpectre.Core.Context;
using VoidSpectre.Core.Interfaces;
using VoidSpectre.Gameplay.Demo;
using UnityEngine;

namespace VoidSpectreUnity.PlayerInput
{
    [RunsInContext(ContextKind.Interior)]
    public sealed class UnityDemoShootInputSystem : ICoreUpdateSystem
    {
        public string Name => "Unity Demo Shoot Input";
        public int Priority => 10;

        public void Update(SimulationContext context, float delta)
        {
            if (!Input.GetKeyDown(KeyCode.S)) return;

            var crew = DemoHierarchySetup.CrewEntity;
            var ship = DemoHierarchySetup.ShipEntity;
            if (!context.Components.Has<DemoCrewTagComponent>(crew)) return;

            context.Events.Enqueue(new DemoShootEvent { Shooter = crew, Target = ship });
        }
    }
}
