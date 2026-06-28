namespace VoidSpectre.Core.Interfaces
{
    public interface IDeepCloneableComponent : ITrackableComponent
    {
        ITrackableComponent DeepClone();
    }
}
