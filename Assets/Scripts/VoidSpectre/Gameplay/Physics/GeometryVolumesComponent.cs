using System;
using System.Collections.Generic;
using Sirenix.Serialization;
using VoidSpectre.Core.Component;
using VoidSpectre.Core.Interfaces;
using VoidSpectre.Core.Math;
using VoidSpectre.Core.Math.Geometry;

namespace VoidSpectre.Gameplay.Physics
{
    [Serializable]
    public sealed class GeometryVolumesComponent : TrackableComponentBase, IDeepCloneableComponent
    {
        [OdinSerialize] private List<GeometryVolume> _volumes = new();

        public List<GeometryVolume> Volumes => _volumes;

        public float TotalVolumeCubicMeters
        {
            get
            {
                float total = 0f;
                for (int i = 0; i < _volumes.Count; i++)
                    total += GeometryVolumeMath.GetVolume(_volumes[i]);
                return total;
            }
        }

        public Float3 CompositeBoundsSize
        {
            get
            {
                VsBounds? bounds = null;
                for (int i = 0; i < _volumes.Count; i++)
                {
                    var volumeBounds = GeometryVolumeMath.GetLocalBounds(_volumes[i]);
                    if (bounds.HasValue)
                    {
                        var b = bounds.Value;
                        b.Encapsulate(volumeBounds);
                        bounds = b;
                    }
                    else
                        bounds = volumeBounds;
                }

                return bounds?.Size ?? Float3.Zero;
            }
        }

        public VsBounds CompositeBounds
        {
            get
            {
                VsBounds? bounds = null;
                for (int i = 0; i < _volumes.Count; i++)
                {
                    var volumeBounds = GeometryVolumeMath.GetLocalBounds(_volumes[i]);
                    if (bounds.HasValue)
                    {
                        var b = bounds.Value;
                        b.Encapsulate(volumeBounds);
                        bounds = b;
                    }
                    else
                        bounds = volumeBounds;
                }

                return bounds ?? new VsBounds(Float3.Zero, Float3.Zero);
            }
        }

        public void SetVolumes(IEnumerable<GeometryVolume> volumes)
        {
            _volumes.Clear();
            _volumes.AddRange(volumes);
            BumpVersion();
        }

        public void AddVolume(GeometryVolume volume)
        {
            _volumes.Add(volume);
            BumpVersion();
        }

        public ITrackableComponent DeepClone()
        {
            var clone = new GeometryVolumesComponent();
            clone.SetVolumes(_volumes);
            return clone;
        }
    }
}
