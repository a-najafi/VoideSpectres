using VoidSpectre.Core.Config;
using VoidSpectre.Core.Math;
using VoidSpectre.Core.Math.Geometry;
using VoidSpectre.Gameplay.Physics;
using VoidSpectre.Gameplay.Ship.Modules;
using VoidSpectre.Gameplay.Ship.Thrusters;

namespace VoidSpectre.Gameplay.Ship.Archetypes
{
    public static class ShipPartArchetypes
    {
        private static bool _registered;

        public static IEntityArchetype Engine { get; private set; }
        public static IEntityArchetype Storage { get; private set; }
        public static IEntityArchetype Cockpit { get; private set; }
        public static IEntityArchetype MainThruster { get; private set; }
        public static IEntityArchetype GimbalThrusterBottom { get; private set; }
        public static IEntityArchetype GimbalThrusterPort { get; private set; }
        public static IEntityArchetype GimbalThrusterStarboard { get; private set; }

        public static void RegisterAll()
        {
            if (_registered) return;
            _registered = true;

            Engine = CreateEngine();
            Storage = CreateStorage();
            Cockpit = CreateCockpit();
            MainThruster = CreateMainThruster();
            GimbalThrusterBottom = CreateGimbalThrusterBottom();
            GimbalThrusterPort = CreateGimbalThrusterPort();
            GimbalThrusterStarboard = CreateGimbalThrusterStarboard();

            EntityArchetypeRegistry.Register(ShipPartArchetypeIds.Engine, Engine);
            EntityArchetypeRegistry.Register(ShipPartArchetypeIds.Storage, Storage);
            EntityArchetypeRegistry.Register(ShipPartArchetypeIds.Cockpit, Cockpit);
            EntityArchetypeRegistry.Register(ShipPartArchetypeIds.MainThruster, MainThruster);
            EntityArchetypeRegistry.Register(ShipPartArchetypeIds.GimbalThrusterBottom, GimbalThrusterBottom);
            EntityArchetypeRegistry.Register(ShipPartArchetypeIds.GimbalThrusterPort, GimbalThrusterPort);
            EntityArchetypeRegistry.Register(ShipPartArchetypeIds.GimbalThrusterStarboard, GimbalThrusterStarboard);
        }

        private static EntityArchetype CreateEngine()
        {
            var geometry = new GeometryVolumesComponent();
            geometry.AddVolume(GeometryVolume.Box(Float3.Zero, new Float3(8f, 6f, 4f)));

            return new EntityArchetype(
                new MassSourceComponent(2_800f),
                geometry,
                new EngineFuelComponent { MaxFuelLiters = 5_000f, CurrentFuelLiters = 5_000f });
        }

        private static EntityArchetype CreateStorage()
        {
            var geometry = new GeometryVolumesComponent();
            geometry.AddVolume(GeometryVolume.Box(Float3.Zero, new Float3(12f, 8f, 10f)));

            return new EntityArchetype(
                new MassSourceComponent(4_500f),
                geometry);
        }

        private static EntityArchetype CreateCockpit()
        {
            var geometry = new GeometryVolumesComponent();
            geometry.AddVolume(GeometryVolume.Box(Float3.Zero, new Float3(6f, 5f, 6f)));

            return new EntityArchetype(
                new MassSourceComponent(1_200f),
                geometry);
        }

        private static EntityArchetype CreateMainThruster()
        {
            var geometry = new GeometryVolumesComponent();
            geometry.AddVolume(GeometryVolume.Cylinder(Float3.Zero, 1.2f, 2.5f));

            return new EntityArchetype(
                new MassSourceComponent(220f),
                geometry,
                new ThrusterComponent
                {
                    MaxThrustNewtons = 180_000f,
                    RampUpSeconds = 0.8f,
                    FuelLitersPerSecondAtFullPower = 8f,
                });
        }

        private static EntityArchetype CreateGimbalThrusterBottom()
        {
            var geometry = new GeometryVolumesComponent();
            geometry.AddVolume(GeometryVolume.Cylinder(Float3.Zero, 0.35f, 1.2f));

            return new EntityArchetype(
                new MassSourceComponent(60f),
                geometry,
                new ThrusterComponent
                {
                    MaxThrustNewtons = 18_000f,
                    RampUpSeconds = 0.25f,
                    FuelLitersPerSecondAtFullPower = 1.5f,
                },
                new GimbalThrusterComponent
                {
                    GimbalAxisLocal = Float3.Forward,
                    ArcHalfDegrees = 60f,
                    MaxGimbalSpeedDegreesPerSecond = 45f
                });
        }

        private static EntityArchetype CreateGimbalThrusterPort()
        {
            var geometry = new GeometryVolumesComponent();
            geometry.AddVolume(GeometryVolume.Cylinder(Float3.Zero, 0.55f, 1.8f));

            return new EntityArchetype(
                new MassSourceComponent(90f),
                geometry,
                new ThrusterComponent
                {
                    MaxThrustNewtons = 45_000f,
                    RampUpSeconds = 0.25f,
                    FuelLitersPerSecondAtFullPower = 2.5f,
                },
                new GimbalThrusterComponent
                {
                    GimbalAxisLocal = Float3.Forward,
                    ArcHalfDegrees = 75f,
                    MaxGimbalSpeedDegreesPerSecond = 35f
                });
        }

        private static EntityArchetype CreateGimbalThrusterStarboard()
        {
            var geometry = new GeometryVolumesComponent();
            geometry.AddVolume(GeometryVolume.Cylinder(Float3.Zero, 0.55f, 1.8f));

            return new EntityArchetype(
                new MassSourceComponent(90f),
                geometry,
                new ThrusterComponent
                {
                    MaxThrustNewtons = 45_000f,
                    RampUpSeconds = 0.25f,
                    FuelLitersPerSecondAtFullPower = 2.5f,
                },
                new GimbalThrusterComponent
                {
                    GimbalAxisLocal = Float3.Forward,
                    ArcHalfDegrees = 75f,
                    MaxGimbalSpeedDegreesPerSecond = 35f
                });
        }
    }
}
