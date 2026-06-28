using System;
using VoidSpectre.Gameplay.Ship.Config;
using VoidSpectreUnity.Config;

namespace VoidSpectreUnity.ShipEditor
{
    public static class ShipEditorUtility
    {
        public const string EditorVisualChildName = "EditorVisual";

        public static ShipPartsConfigComponent GetOrCreateShipPartsConfig(EntityArchetypeSO shipArchetype)
        {
            if (shipArchetype == null)
                throw new ArgumentNullException(nameof(shipArchetype));

            for (int i = 0; i < shipArchetype.Components.Count; i++)
            {
                if (shipArchetype.Components[i] is ShipPartsConfigComponent config)
                    return config;
            }

            var created = new ShipPartsConfigComponent();
            shipArchetype.Components.Add(created);
            return created;
        }

        public static ShipPartsConfigComponent TryGetShipPartsConfig(EntityArchetypeSO shipArchetype)
        {
            if (shipArchetype == null)
                return null;

            for (int i = 0; i < shipArchetype.Components.Count; i++)
            {
                if (shipArchetype.Components[i] is ShipPartsConfigComponent config)
                    return config;
            }

            return null;
        }
    }
}
