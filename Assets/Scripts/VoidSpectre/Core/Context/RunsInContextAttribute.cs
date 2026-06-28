using System;

namespace VoidSpectre.Core.Context
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class RunsInContextAttribute : Attribute
    {
        public ContextKind[] Kinds { get; }

        public RunsInContextAttribute(params ContextKind[] kinds) => Kinds = kinds;
    }
}
