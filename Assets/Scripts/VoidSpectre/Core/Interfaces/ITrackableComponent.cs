namespace VoidSpectre.Core.Interfaces
{
    public interface ITrackableComponent
    {
        uint LocalVersion { get; }
        void BumpVersion();
    }
}
