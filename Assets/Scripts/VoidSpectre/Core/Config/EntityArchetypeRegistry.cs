using System.Collections.Generic;

namespace VoidSpectre.Core.Config
{
    public static class EntityArchetypeRegistry
    {
        private static readonly Dictionary<string, IEntityArchetype> Archetypes = new();

        public static void Register(string id, IEntityArchetype archetype) =>
            Archetypes[id] = archetype;

        public static bool TryGet(string id, out IEntityArchetype archetype) =>
            Archetypes.TryGetValue(id, out archetype);

        public static void Clear() => Archetypes.Clear();
    }
}
