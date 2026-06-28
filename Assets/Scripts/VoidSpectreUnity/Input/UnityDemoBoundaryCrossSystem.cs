using VoidSpectre.Core.Context;
using VoidSpectre.Core.Interfaces;
using VoidSpectre.Gameplay.Demo;
using UnityEngine;

namespace VoidSpectreUnity.PlayerInput
{
    [RunsInContext(ContextKind.Interior)]
    public sealed class UnityDemoBoundaryCrossSystem : ICoreUpdateSystem
    {
        public string Name => "Unity Demo Boundary Cross";
        public int Priority => 50;

        private bool _requested;

        public void Update(SimulationContext context, float delta)
        {
            if (_requested || !Input.GetKeyDown(KeyCode.M)) return;
            if (!context.Components.Has<DemoCrewTagComponent>(DemoHierarchySetup.CrewEntity)) return;

            var parent = context.Parent;
            if (parent == null) return;

            _requested = true;
            context.Events.Enqueue(new EntityExitContextRequested
            {
                Entity = DemoHierarchySetup.CrewEntity,
                DestinationContextId = parent.Id
            });
        }
    }
}
