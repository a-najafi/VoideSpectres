using UnityEngine;
using VoidSpectre.Core.Math;
using VoidSpectre.Gameplay.Demo;
using VoidSpectre.Gameplay.Ship.Navigation;
using VoidSpectreUnity.Bootstrap;
using VoidSpectreUnity.Conversion;

namespace VoidSpectreUnity.Demo
{
    /// <summary>
    /// Demo helper: feeds this GameObject's world position to the demo ship's autopilot as the
    /// target point. Move the object in the Scene view and the ship replans in real time.
    /// Draws the planned maneuver path from ShipManeuverPlanComponent preview samples.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class ShipAutopilotTargetMB : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameBootstrapMB bootstrap;

        [Header("Target")]
        [SerializeField] private float arrivalRadius = 5f;
        [SerializeField] private float maxApproachSpeed = 50f;

        [Header("Path Gizmo")]
        [SerializeField] private int orientationTickStride = 5;
        [SerializeField] private float orientationTickLength = 8f;

        private Float3 _lastAppliedTarget;
        private bool _hasAppliedTarget;

        private void Start()
        {
            if (bootstrap == null)
                bootstrap = FindObjectOfType<GameBootstrapMB>();

            ApplyGoal();
        }

        private void Update() => ApplyGoal();

        private void ApplyGoal()
        {
            if (bootstrap == null)
                return;

            var game = bootstrap.Bootstrap;
            var sector = game?.Sector;
            if (sector == null)
                return;

            var ship = DemoHierarchySetup.ShipEntity;
            if (ship.Id == 0)
                return;

            if (!sector.Components.TryGet(ship, out ShipNavigationGoalComponent goal))
            {
                goal = new ShipNavigationGoalComponent();
                sector.Components.Set(ship, goal);
            }

            goal.UseLegacyPhasePlanner = true;

            var targetPoint = transform.position.ToFloat3();
            if (_hasAppliedTarget &&
                (targetPoint - _lastAppliedTarget).SqrMagnitude < 0.01f &&
                VsMath.Abs(goal.ArrivalRadius - arrivalRadius) < 0.01f &&
                VsMath.Abs(goal.MaxApproachSpeed - maxApproachSpeed) < 0.01f &&
                goal.Mode == ShipNavigationMode.MoveToPoint)
            {
                return;
            }

            goal.TargetPoint = targetPoint;
            goal.ArrivalRadius = arrivalRadius;
            goal.MaxApproachSpeed = maxApproachSpeed;
            goal.Mode = ShipNavigationMode.MoveToPoint;
            _lastAppliedTarget = targetPoint;
            _hasAppliedTarget = true;
        }

        private void OnDrawGizmos()
        {
            var targetPoint = transform.position;
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.9f);
            Gizmos.DrawWireSphere(targetPoint, arrivalRadius);
            Gizmos.DrawLine(targetPoint - Vector3.up * arrivalRadius, targetPoint + Vector3.up * arrivalRadius);
            Gizmos.DrawLine(targetPoint - Vector3.right * arrivalRadius, targetPoint + Vector3.right * arrivalRadius);
            Gizmos.DrawLine(targetPoint - Vector3.forward * arrivalRadius, targetPoint + Vector3.forward * arrivalRadius);

            DrawPlannedPath();
        }

        private void DrawPlannedPath()
        {
            if (bootstrap == null)
                bootstrap = FindObjectOfType<GameBootstrapMB>();

            var sector = bootstrap?.Bootstrap?.Sector;
            if (sector == null)
                return;

            var ship = DemoHierarchySetup.ShipEntity;
            if (ship.Id == 0)
                return;

            if (!sector.Components.TryGet(ship, out ShipManeuverPlanComponent plan) || !plan.IsValid)
                return;

            var samples = plan.PreviewSamples;
            if (samples == null || samples.Count < 2)
                return;

            Gizmos.color = new Color(0.1f, 1f, 0.5f, 0.95f);
            for (int i = 1; i < samples.Count; i++)
            {
                var a = samples[i - 1].Position.ToUnity();
                var b = samples[i].Position.ToUnity();
                Gizmos.DrawLine(a, b);
            }

            if (orientationTickStride <= 0)
                return;

            Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.85f);
            for (int i = 0; i < samples.Count; i += orientationTickStride)
            {
                var sample = samples[i];
                var origin = sample.Position.ToUnity();
                var forward = sample.Orientation.ToUnity() * Vector3.forward * orientationTickLength;
                Gizmos.DrawLine(origin, origin + forward);
            }
        }
    }
}
