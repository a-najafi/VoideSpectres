namespace VoidSpectre.Core.Context
{
    public interface IInteractionFocusProvider
    {
        SimulationContext FocusedContext { get; }
    }
}
