using VoidSpectre.Core.Math;
using VoidSpectre.Gameplay.Ship.Archetypes;
using VoidSpectre.Gameplay.Ship.Config;

namespace VoidSpectre.Gameplay.Ship.Blueprints
{
    public static class BoxShipPartsConfigPreset
    {
        public static ShipPartsConfigComponent Create()
        {
            ShipPartArchetypes.RegisterAll();

            var config = new ShipPartsConfigComponent();

            config.Parts.Add(new ShipPartPlacement
            {
                Archetype = ShipPartArchetypes.Engine,
                LocalPosition = new Float3(0f, 0f, -6f)
            });

            config.Parts.Add(new ShipPartPlacement
            {
                Archetype = ShipPartArchetypes.Storage,
                LocalPosition = new Float3(0f, 0f, 5f)
            });

            config.Parts.Add(new ShipPartPlacement
            {
                Archetype = ShipPartArchetypes.Cockpit,
                LocalPosition = new Float3(0f, 0f, 14f)
            });

            AddMainThruster(config, new Float3(-2.5f, 2f, -8f));
            AddMainThruster(config, new Float3(2.5f, 2f, -8f));
            AddMainThruster(config, new Float3(-2.5f, -2f, -8f));
            AddMainThruster(config, new Float3(2.5f, -2f, -8f));

            AddGimbalBottom(config, new Float3(-3f, -4.5f, 3f));
            AddGimbalBottom(config, new Float3(3f, -4.5f, 3f));
            AddGimbalBottom(config, new Float3(-3f, -4.5f, 7f));
            AddGimbalBottom(config, new Float3(3f, -4.5f, 7f));

            config.Parts.Add(new ShipPartPlacement
            {
                Archetype = ShipPartArchetypes.GimbalThrusterPort,
                LocalPosition = new Float3(-6.5f, 0f, 5f),
                LocalOrientation = FloatQuaternion.AngleAxis(-90f, Float3.Up),
            });

            config.Parts.Add(new ShipPartPlacement
            {
                Archetype = ShipPartArchetypes.GimbalThrusterStarboard,
                LocalPosition = new Float3(6.5f, 0f, 5f),
                LocalOrientation = FloatQuaternion.AngleAxis(90f, Float3.Up),
            });

            return config;
        }

        private static void AddMainThruster(ShipPartsConfigComponent config, Float3 localPosition)
        {
            config.Parts.Add(new ShipPartPlacement
            {
                Archetype = ShipPartArchetypes.MainThruster,
                LocalPosition = localPosition
            });
        }

        private static void AddGimbalBottom(ShipPartsConfigComponent config, Float3 localPosition)
        {
            config.Parts.Add(new ShipPartPlacement
            {
                Archetype = ShipPartArchetypes.GimbalThrusterBottom,
                LocalPosition = localPosition,
                LocalOrientation = FloatQuaternion.AngleAxis(90f, Float3.Right),
            });
        }
    }
}
