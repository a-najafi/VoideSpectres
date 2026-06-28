using System.Collections.Generic;
using VoidSpectre.Core.Math;

namespace VoidSpectre.Gameplay.Ship.Navigation
{
    public static class ShipPlanSimulationUtility
    {
        public static List<ShipPlanTick> SimulateControls(
            ShipSimState initialState,
            ShipContextSnapshot context,
            IReadOnlyList<ShipControlInput> controlSequence,
            int startTickIndex = 0)
        {
            var ticks = new List<ShipPlanTick>();
            var state = initialState.Clone();
            var tickIndex = startTickIndex;
            var fixedDt = context.FixedDt;

            for (int i = 0; i < controlSequence.Count; i++)
            {
                var controls = controlSequence[i] ?? ShipControlInput.Zero(context.Plant.ThrusterCount);
                state = ShipStepSimulation.StepShipSim(state, controls, context, fixedDt);
                ticks.Add(new ShipPlanTick(tickIndex, controls.Clone(), state.Clone()));
                tickIndex++;
            }

            return ticks;
        }

        public static List<ShipPlanTick> SimulatePrimitive(
            ShipSimState initialState,
            ShipContextSnapshot context,
            ShipManeuverPrimitive primitive,
            int startTickIndex = 0)
        {
            var sequence = new ShipControlInput[primitive.DurationTicks];
            for (int i = 0; i < primitive.DurationTicks; i++)
                sequence[i] = primitive.Controls;

            return SimulateControls(initialState, context, sequence, startTickIndex);
        }

        public static List<ShipPlanTick> AppendTicks(
            List<ShipPlanTick> existing,
            IReadOnlyList<ShipPlanTick> appended)
        {
            if (existing == null)
                existing = new List<ShipPlanTick>();

            for (int i = 0; i < appended.Count; i++)
            {
                var tick = appended[i];
                tick.TickIndex = existing.Count;
                existing.Add(tick);
            }

            return existing;
        }
    }
}
