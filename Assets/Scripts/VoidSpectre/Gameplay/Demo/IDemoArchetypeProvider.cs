using VoidSpectre.Core.Config;

namespace VoidSpectre.Gameplay.Demo
{
    public interface IDemoArchetypeProvider
    {
        IEntityArchetype PlanetArchetype { get; }
        IEntityArchetype SpaceRockArchetype { get; }
        IEntityArchetype ShipArchetype { get; }
        IEntityArchetype CrewArchetype { get; }
    }
}
