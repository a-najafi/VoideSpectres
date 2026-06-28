using System.Collections.Generic;
using VoidSpectre.Core.Math;

namespace VoidSpectre.Gameplay.Ship.Navigation
{
    public enum ShipManeuverSegmentKind
    {
        RotateToDirection = 0,
        Accelerate = 1,
        Coast = 2,
        Decelerate = 3,
        Hold = 4,
    }

    public enum ShipPlanExecutionStatus
    {
        Idle = 0,
        Executing = 1,
        Completed = 2,
        Failed = 3,
    }

    public struct ShipManeuverSample
    {
        public float Time;
        public Float3 Position;
        public FloatQuaternion Orientation;

        public ShipManeuverSample(float time, Float3 position, FloatQuaternion orientation)
        {
            Time = time;
            Position = position;
            Orientation = orientation;
        }
    }

    /// <summary>
    /// Control intent stored in world frame so small attitude drift does not rotate thrust incorrectly at replay.
    /// Throttle timeline is the authoritative actuator command during execution.
    /// </summary>
    public struct ShipManeuverControlSample
    {
        public float Time;
        public Float3 DesiredForceWorld;
        public Float3 DesiredTorqueWorld;
        public float[] Throttle;

        public ShipManeuverControlSample(
            float time,
            Float3 desiredForceWorld,
            Float3 desiredTorqueWorld,
            float[] throttle)
        {
            Time = time;
            DesiredForceWorld = desiredForceWorld;
            DesiredTorqueWorld = desiredTorqueWorld;
            Throttle = throttle;
        }
    }

    public struct ShipManeuverSegmentData
    {
        public ShipManeuverSegmentKind Kind;
        public float StartTime;
        public float EndTime;
        public Float3 ReferenceDirectionWorld;
        public float TargetSpeed;
    }

    public enum ShipPlanInvalidationReason
    {
        None = 0,
        GoalChanged = 1,
        TrackingError = 2,
        FuelExhausted = 3,
        ContextHashChanged = 4,
        Collision = 5,
        ManualOverride = 6,
        PlanCompleted = 7,
        ScheduledRefresh = 8,
        PlanningFailed = 9,
    }

    public struct ShipPlanTick
    {
        public int TickIndex;
        public ShipControlInput Controls;
        public ShipSimState ExpectedState;

        public ShipPlanTick(int tickIndex, ShipControlInput controls, ShipSimState expectedState)
        {
            TickIndex = tickIndex;
            Controls = controls;
            ExpectedState = expectedState;
        }
    }
}
