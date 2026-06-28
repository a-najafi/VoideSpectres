using VoidSpectre.Core.Math;
using VoidSpectre.Gameplay.Physics;
using VoidSpectre.Gameplay.Ship.Thrusters;

namespace VoidSpectre.Gameplay.Ship.Thrusters
{
    public static class ThrusterDirectionUtility
    {
        public static Float3 GetShipLocalThrustDirection(
            LocalTransformComponent local,
            GimbalThrusterComponent gimbal)
        {
            var direction = local.LocalRotation * ThrusterComponent.PartLocalThrustDirection;
            if (gimbal != null)
                direction = gimbal.ApplyGimbal(direction);

            return direction.SqrMagnitude > 1e-6f ? direction.Normalized : Float3.Forward;
        }
    }
}
