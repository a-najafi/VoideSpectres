using VoidSpectre.Core.Context;
using VoidSpectre.Core.Interfaces;
using VoidSpectre.Gameplay.Ship.Navigation;
using UnityEngine;

namespace VoidSpectreUnity.PlayerInput
{
    [RunsInContext(ContextKind.Volume)]
    public sealed class UnityShipPilotInputSystem : ICoreUpdateSystem
    {
        public string Name => "Unity Ship Pilot Input";
        public int Priority => 1;

        public void Update(SimulationContext context, float delta)
        {
            foreach (var (_, command) in context.Components.GetAll<ShipPilotCommandComponent>())
            {
                command.MainThrust = Input.GetKey(KeyCode.W) ? 1f : 0f;
                command.Pitch = (Input.GetKey(KeyCode.S) ? -1f : 0f) + (Input.GetKey(KeyCode.X) ? 1f : 0f);
                command.Yaw = (Input.GetKey(KeyCode.A) ? -1f : 0f) + (Input.GetKey(KeyCode.D) ? 1f : 0f);
                command.Roll = (Input.GetKey(KeyCode.Q) ? -1f : 0f) + (Input.GetKey(KeyCode.E) ? 1f : 0f);
            }
        }
    }
}
