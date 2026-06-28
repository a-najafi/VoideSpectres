using System.Collections.Generic;
using VoidSpectre.Core.Math;

namespace VoidSpectre.Core.Context
{
    public sealed class SimulationScheduler
    {
        private readonly SimulationUniverse _universe;
        private IInteractionFocusProvider _focusProvider;

        public SimulationScheduler(SimulationUniverse universe) => _universe = universe;

        public void SetFocusProvider(IInteractionFocusProvider focusProvider) =>
            _focusProvider = focusProvider;

        public void Update(float deltaTime)
        {
            var focused = _focusProvider?.FocusedContext;
            AssignTickTiers(focused);

            foreach (var context in _universe.Contexts)
            {
                var interval = GetTargetInterval(context.CurrentTickTier);
                context.TickAccumulator += deltaTime;

                if (context.CurrentTickTier == TickTier.Dormant)
                    continue;

                if (context.CurrentTickTier != TickTier.Focused && context.TickAccumulator < interval)
                    continue;

                var delta = context.CurrentTickTier == TickTier.Focused
                    ? deltaTime
                    : VsMath.Min(context.TickAccumulator, SimulationUniverse.MaxAccumulatedDelta);

                context.BeginTick();
                context.UpdateCore(delta);
                context.EndTick();

                if (context.CurrentTickTier == TickTier.Focused)
                    context.TickAccumulator = 0f;
                else
                    context.TickAccumulator -= interval;
            }
        }

        private void AssignTickTiers(SimulationContext focused)
        {
            foreach (var context in _universe.Contexts)
                context.CurrentTickTier = TickTier.Background;

            if (focused == null) return;

            focused.CurrentTickTier = TickTier.Focused;

            var ancestor = focused.Parent;
            while (ancestor != null)
            {
                if (ancestor.CurrentTickTier < TickTier.Near)
                    ancestor.CurrentTickTier = TickTier.Near;
                ancestor = ancestor.Parent;
            }

            AssignDescendantsNear(focused);
        }

        private static void AssignDescendantsNear(SimulationContext context)
        {
            foreach (var child in context.Children)
            {
                if (child.CurrentTickTier < TickTier.Near)
                    child.CurrentTickTier = TickTier.Near;
                AssignDescendantsNear(child);
            }
        }

        private static float GetTargetInterval(TickTier tier) => tier switch
        {
            TickTier.Focused => 0f,
            TickTier.Near => 0.1f,
            TickTier.Background => 1f,
            _ => float.PositiveInfinity
        };
    }
}
