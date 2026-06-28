using System;
using System.Collections.Generic;
using Sirenix.Serialization;
using VoidSpectre.Core.Component;
using VoidSpectre.Core;
using VoidSpectre.Core.Context;
using VoidSpectre.Core.Interfaces;

namespace VoidSpectre.Gameplay.Ship.Navigation
{
    public enum ShipPlanningRequestPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3,
    }

    public struct ShipPlanningRequest
    {
        public ComponentStore.EntityId Ship;
        public ShipPlanningRequestPriority Priority;
        public ShipPlanningLodTier RequestedQuality;
        public ShipPlanInvalidationReason Reason;
        public float SubmittedTime;
    }

    [Serializable]
    public sealed class ShipPlanningRequestComponent : TrackableComponentBase
    {
        [OdinSerialize] private bool _hasPendingRequest;
        [OdinSerialize] private ShipPlanningRequestPriority _priority = ShipPlanningRequestPriority.Normal;
        [OdinSerialize] private ShipPlanningLodTier _requestedQuality = ShipPlanningLodTier.Visible;
        [OdinSerialize] private ShipPlanInvalidationReason _reason = ShipPlanInvalidationReason.None;

        public bool HasPendingRequest => _hasPendingRequest;
        public ShipPlanningRequestPriority Priority => _priority;
        public ShipPlanningLodTier RequestedQuality => _requestedQuality;
        public ShipPlanInvalidationReason Reason => _reason;

        public void Submit(
            ShipPlanningRequestPriority priority,
            ShipPlanningLodTier quality,
            ShipPlanInvalidationReason reason)
        {
            _hasPendingRequest = true;
            _priority = priority;
            _requestedQuality = quality;
            _reason = reason;
            BumpVersion();
        }

        public void Clear()
        {
            _hasPendingRequest = false;
            _reason = ShipPlanInvalidationReason.None;
            BumpVersion();
        }
    }

    [RunsInContext(ContextKind.Volume)]
    public sealed class ShipPlanningBudgetSystem : ICoreUpdateSystem
    {
        public const float MaxPlanningMillisecondsPerFrame = 50f;
        public const int MaxHighQualityReplansPerFrame = 1;
        public const int MaxMediumReplansPerFrame = 5;

        public string Name => "Ship Planning Budget";
        public int Priority => 1;

        private readonly List<ShipPlanningRequest> _pending = new();
        private int _frameCounter;
        private int _highQualityUsedThisFrame;
        private int _mediumUsedThisFrame;
        private float _millisecondsUsedThisFrame;

        public void Update(SimulationContext context, float delta)
        {
            _frameCounter++;
            _highQualityUsedThisFrame = 0;
            _mediumUsedThisFrame = 0;
            _millisecondsUsedThisFrame = 0f;
            _pending.Clear();

            foreach (var (ship, request) in context.Components.GetAll<ShipPlanningRequestComponent>())
            {
                if (!request.HasPendingRequest)
                    continue;

                if (!context.Components.TryGet(ship, out ShipPlanningLodComponent lod))
                {
                    lod = new ShipPlanningLodComponent();
                    context.Components.Set(ship, lod);
                }

                if (lod.Tier == ShipPlanningLodTier.Dormant)
                {
                    request.Clear();
                    continue;
                }

                _pending.Add(new ShipPlanningRequest
                {
                    Ship = ship,
                    Priority = request.Priority,
                    RequestedQuality = request.RequestedQuality,
                    Reason = request.Reason,
                });
            }

            if (_pending.Count == 0)
                return;

            _pending.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            for (int i = 0; i < _pending.Count; i++)
            {
                var item = _pending[i];
                if (!CanAfford(item))
                    continue;

                if (!context.Components.TryGet(item.Ship, out ShipNavigationGoalComponent goal) ||
                    !context.Components.TryGet(item.Ship, out ShipManeuverPlanComponent plan) ||
                    !context.Components.TryGet(item.Ship, out ShipPlanExecutionComponent execution))
                {
                    ClearRequest(context, item.Ship);
                    continue;
                }

                var started = System.Diagnostics.Stopwatch.GetTimestamp();
                var built = ShipManeuverPlanner.TryBuildPlan(context, item.Ship, goal, plan, item.RequestedQuality);
                var elapsedMs = (System.Diagnostics.Stopwatch.GetTimestamp() - started) * 1000f /
                                System.Diagnostics.Stopwatch.Frequency;

                _millisecondsUsedThisFrame += elapsedMs;

                if (built)
                {
                    execution.BeginPlan(plan.PlanId, plan.FixedDt);
                    execution.LastInvalidationReason = item.Reason;
                }
                else
                {
                    execution.Reset();
                }

                ChargeBudget(item);
                ClearRequest(context, item.Ship);
            }
        }

        public static void SubmitRequest(
            SimulationContext context,
            ComponentStore.EntityId ship,
            ShipPlanningRequestPriority priority,
            ShipPlanInvalidationReason reason)
        {
            if (!context.Components.TryGet(ship, out ShipPlanningRequestComponent request))
            {
                request = new ShipPlanningRequestComponent();
                context.Components.Set(ship, request);
            }

            var quality = ShipPlanningLodTier.Visible;
            if (context.Components.TryGet(ship, out ShipPlanningLodComponent lod))
                quality = lod.Tier;

            request.Submit(priority, quality, reason);
        }

        private bool CanAfford(ShipPlanningRequest request)
        {
            if (_millisecondsUsedThisFrame >= MaxPlanningMillisecondsPerFrame)
                return false;

            if (request.RequestedQuality >= ShipPlanningLodTier.Hero)
                return _highQualityUsedThisFrame < MaxHighQualityReplansPerFrame;

            if (request.RequestedQuality >= ShipPlanningLodTier.Visible)
                return _mediumUsedThisFrame < MaxMediumReplansPerFrame;

            return true;
        }

        private void ChargeBudget(ShipPlanningRequest request)
        {
            if (request.RequestedQuality >= ShipPlanningLodTier.Hero)
                _highQualityUsedThisFrame++;
            else if (request.RequestedQuality >= ShipPlanningLodTier.Visible)
                _mediumUsedThisFrame++;
        }

        private static void ClearRequest(SimulationContext context, ComponentStore.EntityId ship)
        {
            if (context.Components.TryGet(ship, out ShipPlanningRequestComponent request))
                request.Clear();
        }
    }
}
