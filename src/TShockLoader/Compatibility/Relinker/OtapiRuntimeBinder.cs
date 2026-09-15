using System.Diagnostics;
using System.Reflection;
using MonoMod.RuntimeDetour;
using TerrariaApi.Server;

namespace tShockLoader.Relinker;

public static class OtapiRuntimeBinder
{
    static readonly Dictionary<(MethodBase Method, Delegate Handler), Hook> hooks = [];
    static readonly Dictionary<(string Id, Delegate Handler), (EventInfo Event, Delegate Converted)> tmlEvents = [];

    public static void Add(string id, Delegate handler)
    {
        var method = ResolveMethod(id)
            ?? throw new MissingMethodException($"OTAPI.Runtime hook target not found: {id}");

        var tml = FindTmlOn(method);
        if (tml is not null)
        {
            var converted = Delegate.CreateDelegate(tml.EventHandlerType!, handler.Target, handler.Method, throwOnBindFailure: false);
            if (converted is not null)
            {
                tml.AddEventHandler(null, converted);
                tmlEvents[(id, handler)] = (tml, converted);
                Log($"{id} -> {tml.DeclaringType!.Name}.{tml.Name}");
                return;
            }
        }

        if (!hooks.ContainsKey((method, handler)))
            hooks[(method, handler)] = new Hook(method, handler);
        Log($"{id} -> Hook({method.DeclaringType!.Name}.{method.Name})");
    }

    public static void Remove(string id, Delegate handler)
    {
        if (tmlEvents.Remove((id, handler), out var mapped))
        {
            mapped.Event.RemoveEventHandler(null, mapped.Converted);
            return;
        }

        var method = ResolveMethod(id);
        if (method is not null && hooks.Remove((method, handler), out var hook))
            hook.Dispose();
    }

    internal static void DisposeAll()
    {
        foreach (var (evt, converted) in tmlEvents.Values)
            evt.RemoveEventHandler(null, converted);
        tmlEvents.Clear();

        foreach (var hook in hooks.Values)
            hook.Dispose();
        hooks.Clear();
    }

    static EventInfo? FindTmlOn(MethodBase method)
    {
        var type = method.DeclaringType;
        if (type is null)
            return null;

        var onName = "On_" + type.Name;
        var onType = type.Assembly.GetType($"{type.Namespace}.{onName}") ?? type.Assembly.GetType(onName);
        return onType?.GetEvent(method.Name, BindingFlags.Public | BindingFlags.Static);
    }

    static MethodInfo? ResolveMethod(string id)
    {
        var split = id.Split("::", 2, StringSplitOptions.None);
        if (split.Length != 2)
            return null;

        var type = FindType(split[0]);
        if (type is null)
            return null;

        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        var matches = type.GetMethods(flags).Where(m => m.Name == split[1]).ToArray();
        if (matches.Length == 1)
            return matches[0];
        return type.GetMethod(split[1], flags | BindingFlags.FlattenHierarchy);
    }

    static Type? FindType(string name)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(name);
            if (type is not null)
                return type;
        }

        return null;
    }

    static void Log(string message)
    {
        if (ServerApi.IsBound)
            ServerApi.LogWriter.ServerWriteLine($"runtime-hook {message}", TraceLevel.Info);
    }
}
