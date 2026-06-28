using System;
using Sirenix.Serialization;

namespace VoidSpectre.Core.Context
{
    [Serializable]
    public readonly struct SimulationContextId : IEquatable<SimulationContextId>
    {
        [OdinSerialize] public readonly int Id;

        public SimulationContextId(int id) => Id = id;

        public bool Equals(SimulationContextId other) => Id == other.Id;
        public override bool Equals(object obj) => obj is SimulationContextId other && Equals(other);
        public override int GetHashCode() => Id;
        public override string ToString() => $"Ctx{Id}";

        public static bool operator ==(SimulationContextId a, SimulationContextId b) => a.Id == b.Id;
        public static bool operator !=(SimulationContextId a, SimulationContextId b) => a.Id != b.Id;
    }
}
