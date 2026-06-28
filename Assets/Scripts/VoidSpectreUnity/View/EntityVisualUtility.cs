using UnityEngine;
using VoidSpectre.Core.Config;
using VoidSpectre.Core.Interfaces;
using VoidSpectreUnity.Config;

namespace VoidSpectreUnity.View
{
    public static class EntityVisualUtility
    {
        public static EntityVisualComponent TryGetVisualComponent(EntityArchetypeSO archetype)
        {
            if (archetype?.Components == null)
                return null;

            for (int i = 0; i < archetype.Components.Count; i++)
            {
                if (archetype.Components[i] is EntityVisualComponent visual)
                    return visual;
            }

            return null;
        }

        public static GameObject TryGetPrefab(EntityArchetypeSO archetype)
        {
            var visual = TryGetVisualComponent(archetype);
            return visual?.Prefab;
        }

        public static GameObject TryGetPrefab(IEntityArchetype archetype) =>
            archetype is EntityArchetypeSO archetypeSo ? TryGetPrefab(archetypeSo) : null;
    }
}
