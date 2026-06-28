using System.Collections.Generic;
using VoidSpectre.Core;
using VoidSpectre.Core.Context;
using VoidSpectre.Core.Math;
using VoidSpectre.Gameplay.Physics;
using VoidSpectre.Gameplay.Ship.Parts;

namespace VoidSpectre.Gameplay.Ship.Thrusters
{
    /// <summary>
    /// One thruster's full-throttle contribution to the ship's body-frame wrench.
    /// ForceLocal is in Newtons, TorqueLocal in Newton-metres, both about the centre of mass.
    /// </summary>
    public readonly struct ThrusterWrenchRow
    {
        public readonly ComponentStore.EntityId Entity;
        public readonly Float3 ForceLocal;
        public readonly Float3 TorqueLocal;

        public ThrusterWrenchRow(ComponentStore.EntityId entity, Float3 forceLocal, Float3 torqueLocal)
        {
            Entity = entity;
            ForceLocal = forceLocal;
            TorqueLocal = torqueLocal;
        }
    }

    /// <summary>
    /// Builds the body-frame effectiveness rows (the columns of the control-allocation matrix B)
    /// for a ship's thrusters from their live position, orientation and gimbal state.
    /// </summary>
    public static class ShipThrustModel
    {
        public static void BuildRows(
            SimulationContext context,
            ComponentStore.EntityId ship,
            Float3 centerOfMassLocal,
            List<ThrusterWrenchRow> rows)
        {
            rows.Clear();

            foreach (var (entity, part) in context.Components.GetAll<ShipPartComponent>())
            {
                if (part.ParentShip != ship) continue;
                if (!context.Components.TryGet(entity, out ThrusterComponent thruster)) continue;
                if (!context.Components.TryGet(entity, out LocalTransformComponent local)) continue;
                if (thruster.MaxThrustNewtons <= 0f) continue;

                context.Components.TryGet(entity, out GimbalThrusterComponent gimbal);

                var dirLocal = ThrusterDirectionUtility.GetShipLocalThrustDirection(local, gimbal);
                var forceLocal = dirLocal * thruster.MaxThrustNewtons;
                var lever = local.LocalPosition - centerOfMassLocal;
                var torqueLocal = Float3.Cross(lever, forceLocal);

                rows.Add(new ThrusterWrenchRow(entity, forceLocal, torqueLocal));
            }
        }

        /// <summary>
        /// Maximum force (Newtons) the thruster set can produce along a unit body-frame direction,
        /// assuming each helpful thruster runs at full throttle.
        /// </summary>
        public static float ForceCapacityAlong(IReadOnlyList<ThrusterWrenchRow> rows, Float3 unitDirLocal)
        {
            var capacity = 0f;
            for (int i = 0; i < rows.Count; i++)
            {
                var projected = Float3.Dot(rows[i].ForceLocal, unitDirLocal);
                if (projected > 0f) capacity += projected;
            }

            return capacity;
        }

        /// <summary>
        /// Maximum torque (Newton-metres) the thruster set can produce about a unit body-frame axis,
        /// assuming each helpful thruster runs at full throttle.
        /// </summary>
        public static float TorqueCapacityAbout(IReadOnlyList<ThrusterWrenchRow> rows, Float3 unitAxisLocal)
        {
            var capacity = 0f;
            for (int i = 0; i < rows.Count; i++)
            {
                var projected = Float3.Dot(rows[i].TorqueLocal, unitAxisLocal);
                if (projected > 0f) capacity += projected;
            }

            return capacity;
        }
    }
}
