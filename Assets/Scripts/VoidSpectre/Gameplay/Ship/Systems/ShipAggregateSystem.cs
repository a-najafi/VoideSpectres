using System.Collections.Generic;
using VoidSpectre.Core;
using VoidSpectre.Core.Context;
using VoidSpectre.Core.Diagnostics;
using VoidSpectre.Core.Interfaces;
using VoidSpectre.Core.Math;
using VoidSpectre.Gameplay.Physics;
using VoidSpectre.Gameplay.Ship.Bootstrap;
using VoidSpectre.Gameplay.Ship.Parts;

namespace VoidSpectre.Gameplay.Ship.Systems
{
    [RunsInContext(ContextKind.Volume)]
    public sealed class ShipAggregateSystem : ICoreUpdateSystem
    {
        private readonly HashSet<int> _zeroMassLogged = new();

        public string Name => "Ship Aggregate";
        public int Priority => 0;

        public void Update(SimulationContext context, float delta)
        {
            foreach (var (ship, aggregate) in context.Components.GetAll<ShipAggregateComponent>())
                RebuildFromParts(context, ship, aggregate);
        }

        private void RebuildFromParts(
            SimulationContext context,
            ComponentStore.EntityId ship,
            ShipAggregateComponent aggregate)
        {
            float totalMass = 0f;
            float totalVolume = 0f;
            Float3 weightedCom = Float3.Zero;
            VsBounds? compositeBounds = null;

            foreach (var (entity, part) in context.Components.GetAll<ShipPartComponent>())
            {
                if (part.ParentShip != ship) continue;
                if (!ShipEntityBootstrapUtility.TryGetPartMassKg(context, entity, out var mass)) continue;
                if (!context.Components.TryGet(entity, out GeometryVolumesComponent geometry)) continue;
                if (!context.Components.TryGet(entity, out LocalTransformComponent local)) continue;
                if (mass <= 0f) continue;

                totalMass += mass;
                totalVolume += geometry.TotalVolumeCubicMeters;
                weightedCom += local.LocalPosition * mass;

                var partBounds = geometry.CompositeBounds;
                partBounds.Center += local.LocalPosition;
                if (compositeBounds.HasValue)
                {
                    var b = compositeBounds.Value;
                    b.Encapsulate(partBounds);
                    compositeBounds = b;
                }
                else
                    compositeBounds = partBounds;
            }

            if (totalMass <= 0f)
            {
                aggregate.TotalMassKg = 0f;
                aggregate.TotalVolumeCubicMeters = 0f;
                aggregate.ApproximateMomentOfInertia = 0.01f;
                context.Components.Set(ship, new MassComponent(0f));

                if (_zeroMassLogged.Add(ship.Id))
                {
                    VsLog.Warning(
                        $"[ShipAggregate] Ship {ship} has zero mass from parts. " +
                        "Add MassSourceComponent to each part archetype so planning can use ship mass.");
                }

                return;
            }

            aggregate.TotalMassKg = totalMass;
            aggregate.TotalVolumeCubicMeters = totalVolume;
            aggregate.CenterOfMassLocal = weightedCom / totalMass;
            aggregate.CompositeBoundsSize = compositeBounds?.Size ?? Float3.Zero;

            var size = aggregate.CompositeBoundsSize;
            aggregate.ApproximateMomentOfInertia = totalMass * size.SqrMagnitude / 12f;
            context.Components.Set(ship, new MassComponent(totalMass));
        }
    }
}
