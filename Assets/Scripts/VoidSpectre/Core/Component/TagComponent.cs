using VoidSpectre.Core.Interfaces;

namespace VoidSpectre.Core.Component
{
    public abstract class TagComponent : ITrackableComponent
    {
        public uint LocalVersion { get; private set; }
        public void BumpVersion() { }
    }
}
