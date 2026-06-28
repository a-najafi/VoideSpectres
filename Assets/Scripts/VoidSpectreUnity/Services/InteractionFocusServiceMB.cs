using VoidSpectre.Core.Context;
using VoidSpectre.Core.Diagnostics;
using VoidSpectreUnity.Services;
using UnityEngine;

namespace VoidSpectreUnity.Services
{
    public sealed class InteractionFocusServiceMB : SceneServiceBehaviour<InteractionFocusServiceMB>, ISettableInteractionFocus
    {
        public SimulationUniverse Universe { get; private set; }
        public SimulationContext FocusedContext { get; private set; }

        public void Initialize(SimulationUniverse universe)
        {
            Universe = universe;
        }

        public void SetFocus(SimulationContext context)
        {
            if (context == null || context.Universe != Universe) return;
            FocusedContext = context;
            VsLog.Info(
                $"[Focus] Now focused on: {context.DisplayName} ({context.Kind}). " +
                (context.Kind == ContextKind.Volume
                    ? "Space sim at full tick rate — W thrust active."
                    : "Interior focus — sector sim runs at reduced rate; press 2 for space."));
        }

        private void Update()
        {
            if (Universe == null) return;

            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1))
            {
                foreach (var ctx in Universe.Contexts)
                {
                    if (ctx.Kind == ContextKind.Interior)
                        SetFocus(ctx);
                }
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2))
            {
                foreach (var ctx in Universe.Contexts)
                {
                    if (ctx.Kind == ContextKind.Volume && ctx.Parent == null)
                        SetFocus(ctx);
                }
            }
        }
    }
}
