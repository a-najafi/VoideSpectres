using VoidSpectre.Core.Interfaces;

namespace VoidSpectre.Core.Config
{
    /// <summary>
    /// Marker for components that describe setup data and are expanded into runtime
    /// components/entities by a resolver system, then removed from the entity.
    /// </summary>
    public interface IConfigComponent : ITrackableComponent { }
}
