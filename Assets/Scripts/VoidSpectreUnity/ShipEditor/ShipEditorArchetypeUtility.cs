using VoidSpectre.Core.Interfaces;
using VoidSpectre.Gameplay.Ship.Thrusters;
using VoidSpectreUnity.Config;

namespace VoidSpectreUnity.ShipEditor
{
    public static class ShipEditorArchetypeUtility
    {
        public static bool HasThruster(EntityArchetypeSO archetype)
        {
            if (archetype?.Components == null)
                return false;

            for (int i = 0; i < archetype.Components.Count; i++)
            {
                if (archetype.Components[i] is ThrusterComponent)
                    return true;
            }

            return false;
        }

        public static bool HasComponent<T>(EntityArchetypeSO archetype) where T : class, ITrackableComponent
        {
            if (archetype?.Components == null)
                return false;

            for (int i = 0; i < archetype.Components.Count; i++)
            {
                if (archetype.Components[i] is T)
                    return true;
            }

            return false;
        }
    }
}
