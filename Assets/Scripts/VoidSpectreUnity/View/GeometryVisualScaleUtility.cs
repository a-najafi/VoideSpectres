using UnityEngine;
using VoidSpectre.Core;
using VoidSpectre.Core.Context;
using VoidSpectre.Core.Math.Geometry;
using VoidSpectre.Gameplay.Physics;
using VoidSpectreUnity.Config;
using VoidSpectreUnity.Conversion;

namespace VoidSpectreUnity.View
{
    public static class GeometryVisualScaleUtility
    {
        public static bool TryGetVisualScale(EntityArchetypeSO archetype, out Vector3 scale)
        {
            scale = Vector3.one;
            if (archetype?.Components == null)
                return false;

            GeometryVolumesComponent geometry = null;
            for (int i = 0; i < archetype.Components.Count; i++)
            {
                if (archetype.Components[i] is GeometryVolumesComponent found)
                {
                    geometry = found;
                    break;
                }
            }

            if (geometry?.Volumes == null || geometry.Volumes.Count == 0)
                return false;

            return TryGetVisualScale(geometry.Volumes[0], out scale);
        }

        public static bool TryGetVisualScale(
            SimulationContext context,
            ComponentStore.EntityId entity,
            out Vector3 scale)
        {
            scale = Vector3.one;
            if (context == null)
                return false;

            if (!context.Components.TryGet(entity, out GeometryVolumesComponent geometry))
                return false;

            if (geometry.Volumes == null || geometry.Volumes.Count == 0)
                return false;

            return TryGetVisualScale(geometry.Volumes[0], out scale);
        }

        public static bool TryGetVisualScale(GeometryVolume volume, out Vector3 scale)
        {
            switch (volume.Shape)
            {
                case GeometryShape.Sphere:
                    scale = Vector3.one * volume.SphereRadius;
                    return true;

                case GeometryShape.Cylinder:
                    scale = new Vector3(volume.CylinderRadius, volume.CylinderRadius, volume.CylinderHeight);
                    return true;

                case GeometryShape.Box:
                default:
                    scale = volume.BoxSize.ToUnity();
                    return true;
            }
        }
    }
}
