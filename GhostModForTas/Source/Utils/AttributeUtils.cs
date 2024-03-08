//#define AttributeDebug
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TAS.Input.Commands;

namespace Celeste.Mod.GhostModForTas.Utils.Attributes;

internal static class AttributeUtils {
    private static readonly object[] Parameterless = { };
    internal static readonly IDictionary<Type, IEnumerable<MethodInfo>> MethodInfos = new Dictionary<Type, IEnumerable<MethodInfo>>();

#if AttributeDebug
    public static string exceptionClass = "";
    public static Dictionary<MethodInfo, Type> debugDict = new();
    public static void CollectMethods<T>() where T : Attribute {
        typeof(AttributeUtils).Assembly.GetTypesSafe().ToList().ForEach(type => type
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(info => info.GetParameters().Length == 0 && info.GetCustomAttribute<T>() != null)
            .ToList().ForEach(method => debugDict[method] = type));

        if (exceptionClass.IsNullOrEmpty()) {
            MethodInfos[typeof(T)] = typeof(AttributeUtils).Assembly.GetTypesSafe().SelectMany(type => type
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(info => info.GetParameters().Length == 0 && info.GetCustomAttribute<T>() != null));
            return;
        }

        MethodInfos[typeof(T)] = typeof(AttributeUtils).Assembly.GetTypesSafe().Where(type => !type.FullName.StartsWith(exceptionClass)).SelectMany(type => type
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(info => info.GetParameters().Length == 0 && info.GetCustomAttribute<T>() != null));
    }

    public static void Invoke<T>() where T : Attribute {
        if (MethodInfos.TryGetValue(typeof(T), out var methodInfos)) {
            foreach (MethodInfo methodInfo in methodInfos) {
                try {
                    methodInfo.Invoke(null, Parameterless);
                }
                catch {
                    Celeste.Commands.Log($"AttributeUtils Invoke {debugDict[methodInfo]}.{methodInfo} failed");
                }
            }
        }
    }
#else
    public static void CollectMethods<T>() where T : Attribute {
        MethodInfos[typeof(T)] = typeof(AttributeUtils).Assembly.GetTypesSafe().SelectMany(type => type
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(info => info.GetParameters().Length == 0 && info.GetCustomAttribute<T>() != null));
    }

    public static void Invoke<T>() where T : Attribute {
        if (MethodInfos.TryGetValue(typeof(T), out var methodInfos)) {
            foreach (MethodInfo methodInfo in methodInfos) {
                methodInfo.Invoke(null, Parameterless);
            }
        }
    }
#endif

    public static void SendToTas<TSource>(string attributeFullName) where TSource : Attribute {
        if (ModUtils.GetType("CelesteTAS", attributeFullName) is not { } target) {
            return;
        }
        if (MethodInfos.TryGetValue(typeof(TSource), out var sourceInfos) && TAS.Utils.AttributeUtils.MethodInfos.TryGetValue(target, out var targetInfos)) {
            targetInfos = targetInfos.ToList().Apply(x => x.AddRange(sourceInfos)).Distinct();
            MethodsSentToTas.Add(sourceInfos, target);
        }
    }

    [Unload]
    public static void ReclaimFromTas() {
        foreach (KeyValuePair<IEnumerable<MethodInfo>, Type> pair in MethodsSentToTas) {
            if (TAS.Utils.AttributeUtils.MethodInfos.TryGetValue(pair.Value, out var targetInfos)) {
                targetInfos = targetInfos.ToList().Apply(x => {
                    foreach (MethodInfo source in pair.Key) {
                        x.Remove(source);
                    }
                });
            }
        }

        IEnumerable<MethodInfo> localMethodInfos = typeof(AttributeUtils).Assembly.GetTypesSafe().SelectMany(type => type
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(info => info.GetCustomAttributes<TasCommandAttribute>().IsNotEmpty());
        IDictionary<TasCommandAttribute, MethodInfo> tasMethodInfos = TasCommandAttribute.MethodInfos;
        foreach (MethodInfo methodInfo in localMethodInfos) {
            IEnumerable<TasCommandAttribute> tasCommandAttributes = methodInfo.GetCustomAttributes<TasCommandAttribute>();
            foreach (TasCommandAttribute tasCommandAttribute in tasCommandAttributes) {
                tasMethodInfos.Remove(tasCommandAttribute);
            }
        }
    }

    private static readonly Dictionary<IEnumerable<MethodInfo>, Type> MethodsSentToTas = new();

    [Initialize]
    public static void CollectAndSendTasCommand() {
        IEnumerable<MethodInfo> localMethodInfos = typeof(AttributeUtils).Assembly.GetTypesSafe().SelectMany(type => type
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(info => info.GetCustomAttributes<TasCommandAttribute>().IsNotEmpty());
        IDictionary<TasCommandAttribute, MethodInfo> tasMethodInfos = TasCommandAttribute.MethodInfos;
        foreach (MethodInfo methodInfo in localMethodInfos) {
            IEnumerable<TasCommandAttribute> tasCommandAttributes = methodInfo.GetCustomAttributes<TasCommandAttribute>();
            foreach (TasCommandAttribute tasCommandAttribute in tasCommandAttributes) {
                tasMethodInfos[tasCommandAttribute] = methodInfo;
            }
        }
    }

    [Initialize]
    public static void HookEngineFreeze() {
        typeof(Monocle.Engine).GetMethodInfo("Update").IlHook(ILEngineFreeze);
    }

    public static void ILEngineFreeze(ILContext il) {
        ILCursor cursor = new(il);
        if (cursor.TryGotoNext(ins => ins.MatchLdsfld<Monocle.Engine>(nameof(Monocle.Engine.FreezeTimer)), ins => ins.MatchLdcR4(0f))) {
            cursor.Index += 3;
            cursor.MoveAfterLabels();
            cursor.EmitDelegate(InvokeFreezeUpdate);
        }
    }

    private static void InvokeFreezeUpdate() {
        Invoke<FreezeUpdateAttribute>();
    }
}


[AttributeUsage(AttributeTargets.Method)]
internal class LoadAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
internal class UnloadAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
internal class LoadContentAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
internal class InitializeAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
internal class FreezeUpdateAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
internal class TasDisableRunAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
internal class TasEnableRunAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
internal class ReloadAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
// it allows 0 - 3 parameters, so you can't collect/invoke it using attribute utils functions. we put it here just to make it global
internal class LoadLevelAttribute : Attribute {
    public bool Before;

    public LoadLevelAttribute(bool before = false) {
        Before = before;
    }
}