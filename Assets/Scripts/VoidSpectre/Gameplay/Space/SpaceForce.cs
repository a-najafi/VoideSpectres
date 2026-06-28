using System;
using Sirenix.Serialization;
using VoidSpectre.Core.Math;

namespace VoidSpectre.Gameplay.Space
{
    [Serializable]
    public struct SpaceForce
    {
        [OdinSerialize] public Float3 Force;
        [OdinSerialize] public int SourceId;

        public SpaceForce(Float3 force, int sourceId = 0)
        {
            Force = force;
            SourceId = sourceId;
        }
    }
}
