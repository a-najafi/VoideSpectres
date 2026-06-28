using VoidSpectre.Core;
using VoidSpectre.Core.Context;
using VoidSpectre.Core.Math;
using VoidSpectre.Gameplay.Ship.Parts;
using VoidSpectre.Gameplay.Ship.Thrusters;

namespace VoidSpectre.Gameplay.Ship.Navigation
{
    /// <summary>
    /// Immutable context captured at plan time. Any hash mismatch invalidates the plan.
    /// </summary>
    public sealed class ShipContextSnapshot
    {
        public const int IntegratorVersion = 2;

        public float FixedDt;
        public ShipPlantModel Plant;
        public ShipGravityModel Gravity;
        public int ValidityHash;
        public float[] FuelLitersPerSecondAtFullPower;

        public static ShipContextSnapshot Capture(
            SimulationContext context,
            ComponentStore.EntityId ship,
            float fixedDt)
        {
            var plant = ShipPlantModel.Build(context, ship);
            var gravity = ShipGravityModel.TryBuild(context, ship, plant.MassKg);
            var fuelRates = new float[plant.ThrusterCount];

            for (int i = 0; i < plant.ThrusterCount; i++)
            {
                if (context.Components.TryGet(plant.ThrusterEntities[i], out ThrusterComponent thruster))
                    fuelRates[i] = thruster.FuelLitersPerSecondAtFullPower;
            }

            var snapshot = new ShipContextSnapshot
            {
                FixedDt = fixedDt,
                Plant = plant,
                Gravity = gravity,
                FuelLitersPerSecondAtFullPower = fuelRates,
            };
            snapshot.ValidityHash = snapshot.ComputeValidityHash(context, ship);
            return snapshot;
        }

        public int ComputeValidityHash(SimulationContext context, ComponentStore.EntityId ship)
        {
            var hash = IntegratorVersion;
            hash = HashCombine(hash, FloatHash(FixedDt));

            if (context.Components.TryGet(ship, out ShipAggregateComponent aggregate))
            {
                hash = HashCombine(hash, FloatHash(aggregate.TotalMassKg));
                hash = HashCombine(hash, FloatHash(aggregate.CenterOfMassLocal.X));
                hash = HashCombine(hash, FloatHash(aggregate.CenterOfMassLocal.Y));
                hash = HashCombine(hash, FloatHash(aggregate.CenterOfMassLocal.Z));
                hash = HashCombine(hash, FloatHash(aggregate.ApproximateMomentOfInertia));
            }

            hash = HashCombine(hash, Plant.ThrusterCount);

            return hash;
        }

        public bool IsStructurallyValid(SimulationContext context, ComponentStore.EntityId ship) =>
            ValidityHash == ComputeValidityHash(context, ship);

        [System.Obsolete("Use IsStructurallyValid; fuel changes during execution are expected.")]
        public bool IsStillValid(SimulationContext context, ComponentStore.EntityId ship) =>
            IsStructurallyValid(context, ship);

        private static int HashCombine(int hash, int value) => hash * 31 + value;

        private static int FloatHash(float value) => value.GetHashCode();
    }
}
