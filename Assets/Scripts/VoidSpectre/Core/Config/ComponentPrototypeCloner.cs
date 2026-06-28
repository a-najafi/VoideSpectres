using System;
using System.Reflection;
using VoidSpectre.Core.Interfaces;

namespace VoidSpectre.Core.Config
{
    public static class ComponentPrototypeCloner
    {
        public static ITrackableComponent Clone(ITrackableComponent source)
        {
            if (source is IDeepCloneableComponent deepCloneable)
                return deepCloneable.DeepClone();

            var clone = (ITrackableComponent)Activator.CreateInstance(source.GetType());
            CopyFields(source, clone);
            return clone;
        }

        private static void CopyFields(object source, object target)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var type = source.GetType();

            while (type != null && type != typeof(object))
            {
                foreach (var field in type.GetFields(flags))
                {
                    if (field.IsStatic || field.IsInitOnly) continue;
                    field.SetValue(target, field.GetValue(source));
                }

                type = type.BaseType;
            }
        }
    }
}
