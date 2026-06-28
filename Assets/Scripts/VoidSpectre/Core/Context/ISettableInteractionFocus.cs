namespace VoidSpectre.Core.Context
{
    public interface ISettableInteractionFocus : IInteractionFocusProvider
    {
        void SetFocus(SimulationContext context);
    }
}
