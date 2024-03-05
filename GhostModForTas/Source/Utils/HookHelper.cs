using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Celeste.Mod.GhostModForTas.Utils;
internal static class HookHelper {
    // taken from CelesteTAS
    private static readonly List<IDetour> Hooks = new();

    public static ILHookConfig manualConfig = default;

    internal static void InitializeAtFirst() {
        manualConfig.ManualApply = true;
    }

    public static void Unload() {
        foreach (IDetour detour in Hooks) {
            detour.Dispose();
        }

        Hooks.Clear();
    }

    // check https://jatheplayer.github.io/celeste/ilhookview/ before creating a hook, to avoid conflict

    public static void OnHook(this MethodBase from, Delegate to) {
        Hooks.Add(new Hook(from, to));
    }

    public static void IlHook(this MethodBase from, ILContext.Manipulator manipulator) {
        Hooks.Add(new ILHook(from, manipulator));
    }

    public static void IlHook(this MethodBase from, Action<ILCursor, ILContext> manipulator) {
        from.IlHook(il => {
            ILCursor ilCursor = new(il);
            manipulator(ilCursor, il);
        });
    }

    public static void HookBefore<T>(this MethodBase methodInfo, Action<T> action) {
        methodInfo.IlHook((cursor, _) => {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate(action);
        });
    }

    public static void HookBefore(this MethodBase methodInfo, Action action) {
        methodInfo.IlHook((cursor, _) => {
            cursor.EmitDelegate(action);
        });
    }

    public static void HookAfter<T>(this MethodBase methodInfo, Action<T> action) {
        methodInfo.IlHook((cursor, _) => {
            while (cursor.TryGotoNext(MoveType.AfterLabel, i => i.OpCode == OpCodes.Ret)) {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate(action);
                cursor.Index++;
            }
        });
    }

    public static void HookAfter(this MethodBase methodInfo, Action action) {
        methodInfo.IlHook((cursor, _) => {
            while (cursor.TryGotoNext(MoveType.AfterLabel, i => i.OpCode == OpCodes.Ret)) {
                cursor.EmitDelegate(action);
                cursor.Index++;
            }
        });
    }

    public static void SkipMethod(Type conditionType, string conditionMethodName, string methodName, params Type[] types) {
        foreach (Type type in types) {
            if (type?.GetMethodInfo(methodName) is { } method) {
                SkipMethod(conditionType, conditionMethodName, method);
            }
        }
    }

    public static void SkipMethod(Type conditionType, string conditionMethodName, params MethodInfo[] methodInfos) {
        foreach (MethodInfo methodInfo in methodInfos) {
            methodInfo.IlHook(il => {
                ILCursor ilCursor = new(il);
                Instruction start = ilCursor.Next;
                ilCursor.Emit(OpCodes.Call, conditionType.GetMethodInfo(conditionMethodName));
                ilCursor.Emit(OpCodes.Brfalse, start).Emit(OpCodes.Ret);
            });
        }
    }

    public static void ReturnZeroMethod(Type conditionType, string conditionMethodName, params MethodInfo[] methods) {
        foreach (MethodInfo methodInfo in methods) {
            if (methodInfo != null && !methodInfo.IsGenericMethod && methodInfo.DeclaringType?.IsGenericType != true &&
                methodInfo.ReturnType == typeof(float)) {
                methodInfo.IlHook(il => {
                    ILCursor ilCursor = new(il);
                    Instruction start = ilCursor.Next;
                    ilCursor.Emit(OpCodes.Call, conditionType.GetMethodInfo(conditionMethodName));
                    ilCursor.Emit(OpCodes.Brfalse, start).Emit(OpCodes.Ldc_R4, 0f).Emit(OpCodes.Ret);
                });
            }
        }
    }
}

public static class CILCodeHelper {
    public static void CILCodeLogger(this ILCursor ilCursor, int logCount = 999999, bool useCommand = true) {
        // remember, Commands.Log can only work in Initialize()
        if (useCommand) {
            Celeste.Commands.Log("------------------------------");
        }
        Logger.Log(LogLevel.Debug, "Ghost Mod For Tas", "---- CILCodeLogger ----");
        if (Apply) {
            if (AsShift) {
                ilCursor.Index += Position;
            } else {
                ilCursor.Index = Position;
            }
        }
        while (logCount > 0 && ilCursor.Next is not null) {
            string str;
            if (ilCursor.Next.Operand is ILLabel label) {
                str = $"{ilCursor.Next.Offset:x4}, {ilCursor.Next.OpCode}, {ilCursor.Next.Operand} | {label.Target.Offset:x4}, {label.Target.OpCode}, {label.Target.Operand}";
            } else if (ilCursor.Next.Operand is Instruction ins) {
                str = $"{ilCursor.Next.Offset:x4}, {ilCursor.Next.OpCode} | {ins.Offset:x4}, {ins.OpCode}, {ins.Operand}";
            } else {
                str = $"{ilCursor.Next.Offset:x4}, {ilCursor.Next.OpCode}, {ilCursor.Next.Operand}";
            }
            Mod.Logger.Log(LogLevel.Debug, "Ghost Mod For Tas", str);
            if (useCommand) {
                Celeste.Commands.Log(str);
            }
            logCount--;
            ilCursor.Index++;
        }
    }

    public static void CILCodeLogger(this MethodBase methodBase, int logCount = 999999, bool useCommand = true) {
        new ILHook(methodBase, il => {
            ILCursor cursor = new ILCursor(il);
            CILCodeLogger(cursor, logCount, useCommand);
        }).Dispose();
    }

    public static void CILCodeLoggerAtLast(this MethodBase methodBase) {
        methods.Add(methodBase);
    }

    internal static void InitializeAtLast() {
        using (DetourContext context = new DetourContext() { After = new List<string>() { "*" } }) {
            foreach (MethodBase method in methods) {
                method.CILCodeLogger();
            }
        }
        methods.Clear();
    }

    private static List<MethodBase> methods = new();

    private static int position = 0;

    public static int Position {
        get => position;
        set {
            position = value;
            Apply = true;
        }
    }

    public static bool Apply = false;

    public static bool AsShift = true;
}

