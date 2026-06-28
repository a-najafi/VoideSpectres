using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VoidSpectre.Core.Context;
using VoidSpectre.Core.Diagnostics;
using VoidSpectre.Core.Interfaces;

namespace VoidSpectre.Core
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class AutoRegisterIgnoreAttribute : Attribute { }

    public static class AutoSystemRegistry
    {
        public static void RegisterAll(SimulationContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var types = CollectCandidates(assemblies);

            int candidates = 0, instantiated = 0, registered = 0;

            for (int t = 0; t < types.Count; t++)
            {
                var type = types[t];
                candidates++;

                if (!MatchesContextKind(type, context.Kind))
                    continue;

                if (!TryCreateInstance(type, out var instance))
                {
                    VsLog.Warning("[AutoSys] No suitable ctor for " + type.FullName);
                    continue;
                }

                instantiated++;
                bool didAny = false;

                if (typeof(ICoreUpdateSystem).IsAssignableFrom(type))
                {
                    try
                    {
                        context.RegisterCoreSystem((ICoreUpdateSystem)instance);
                        didAny = true;
                        registered++;
                    }
                    catch (Exception ex)
                    {
                        VsLog.Exception(ex);
                    }
                }

                var compIfaces = type.GetInterfaces();
                for (int i = 0; i < compIfaces.Length; i++)
                {
                    var iface = compIfaces[i];
                    if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IComponentChangeSystem<>))
                    {
                        var genArg = iface.GetGenericArguments()[0];
                        try
                        {
                            var m = typeof(AutoSystemRegistry).GetMethod(
                                nameof(RegisterChangeGeneric),
                                BindingFlags.NonPublic | BindingFlags.Static);
                            m.MakeGenericMethod(genArg).Invoke(null, new object[] { context, instance });
                            didAny = true;
                            registered++;
                        }
                        catch (Exception ex)
                        {
                            VsLog.Exception(ex);
                        }
                    }
                }

                if (ImplementsOpenGeneric(type, typeof(IEventSystem<>)))
                {
                    try
                    {
                        context.RegisterEventSystem((ISystem)instance);
                        didAny = true;
                        registered++;
                    }
                    catch (Exception ex)
                    {
                        VsLog.Exception(ex);
                    }
                }

                if (!didAny)
                    VsLog.Info("[AutoSys] Skipped (no recognized interfaces): " + type.FullName);
            }

            VsLog.Info($"[AutoSys] Context {context.DisplayName}: Candidates={candidates}, Instantiated={instantiated}, Registered={registered}");
        }

        private static bool MatchesContextKind(Type type, ContextKind contextKind)
        {
            var attr = type.GetCustomAttribute<RunsInContextAttribute>();
            if (attr == null || attr.Kinds == null || attr.Kinds.Length == 0)
                return true;
            return attr.Kinds.Contains(contextKind);
        }

        private static List<Type> CollectCandidates(Assembly[] assemblies)
        {
            var list = new List<Type>(256);

            for (int a = 0; a < assemblies.Length; a++)
            {
                Type[] types;
                try { types = assemblies[a].GetTypes(); }
                catch (ReflectionTypeLoadException ex) { types = ex.Types; }

                if (types == null) continue;

                for (int i = 0; i < types.Length; i++)
                {
                    var t = types[i];
                    if (t == null) continue;
                    if (!t.IsClass || t.IsAbstract || t.IsGenericTypeDefinition) continue;
                    if (t.GetCustomAttributes(typeof(AutoRegisterIgnoreAttribute), false).Length > 0) continue;
                    if (!typeof(ISystem).IsAssignableFrom(t)) continue;

                    if (typeof(ICoreUpdateSystem).IsAssignableFrom(t) ||
                        ImplementsOpenGeneric(t, typeof(IEventSystem<>)) ||
                        ImplementsOpenGeneric(t, typeof(IComponentChangeSystem<>)))
                    {
                        list.Add(t);
                    }
                }
            }

            return list;
        }

        private static bool ImplementsOpenGeneric(Type t, Type openGeneric)
        {
            var ifaces = t.GetInterfaces();
            for (int i = 0; i < ifaces.Length; i++)
            {
                var it = ifaces[i];
                if (it.IsGenericType && it.GetGenericTypeDefinition() == openGeneric) return true;
            }

            return false;
        }

        private static void RegisterChangeGeneric<T>(SimulationContext context, object sys)
            where T : class, ITrackableComponent
        {
            context.RegisterComponentChangeSystem((IComponentChangeSystem<T>)sys);
        }

        private static bool TryCreateInstance(Type t, out object instance)
        {
            instance = null;

            var empty = t.GetConstructor(Type.EmptyTypes);
            if (empty != null)
            {
                try
                {
                    instance = Activator.CreateInstance(t);
                    return true;
                }
                catch { }
            }

            var ctors = t.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .OrderByDescending(c => c.GetParameters().Length)
                .ToArray();

            for (int c = 0; c < ctors.Length; c++)
            {
                var ctor = ctors[c];
                var ps = ctor.GetParameters();
                if (ps.Length == 0) continue;

                var args = new object[ps.Length];
                bool ok = true;

                for (int i = 0; i < ps.Length; i++)
                {
                    var p = ps[i];
                    if (p.IsOptional) { args[i] = p.DefaultValue; continue; }
                    if (p.ParameterType.IsValueType) { args[i] = Activator.CreateInstance(p.ParameterType); continue; }
                    ok = false;
                    break;
                }

                if (!ok) continue;

                try
                {
                    instance = ctor.Invoke(args);
                    return true;
                }
                catch { }
            }

            return false;
        }
    }
}
