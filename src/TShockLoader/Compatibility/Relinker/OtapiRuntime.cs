using System.Reflection;
using System.Runtime.Loader;
using Mono.Cecil;
using Mono.Cecil.Cil;
using CecilEventAttributes = Mono.Cecil.EventAttributes;
using CecilMethodAttributes = Mono.Cecil.MethodAttributes;
using CecilMethodImplAttributes = Mono.Cecil.MethodImplAttributes;
using CecilTypeAttributes = Mono.Cecil.TypeAttributes;

namespace tShockLoader.Relinker;

static class OtapiRuntime
{
    static readonly HashSet<string> requested = new(StringComparer.Ordinal);
    static readonly List<Node> nodes = [];
    static Assembly? assembly;

    public static Assembly? Assembly => assembly;

    public static void Request(TypeReference type)
    {
        for (var current = type; current is not null; current = current.DeclaringType)
        {
            if (!requested.Add(current.FullName))
                return;

            var name = current.Name;
            var isOrig = name.StartsWith("orig_", StringComparison.Ordinal);
            var isHook = name.StartsWith("hook_", StringComparison.Ordinal);
            nodes.Add(new Node
            {
                FullName = current.FullName,
                Name = name,
                Namespace = current.Namespace ?? "",
                Parent = current.DeclaringType?.FullName,
                IsDelegate = isOrig || isHook,
                IsHook = isHook,
                MethodName = isOrig || isHook ? name[(name.IndexOf('_') + 1)..] : "",
            });
        }
    }

    public static void Install()
    {
        if (assembly is not null || nodes.Count == 0)
            return;

        using var pe = new MemoryStream();
        using (var definition = Emit())
            definition.Write(pe);

        pe.Position = 0;
        var context = AssemblyLoadContext.GetLoadContext(typeof(OtapiRuntime).Assembly)
            ?? throw new InvalidOperationException("tShockLoader assembly has no load context.");
        assembly = context.LoadFromStream(pe);
    }

    static AssemblyDefinition Emit()
    {
        var name = new AssemblyNameDefinition("OTAPI.Runtime", new Version(0, 0, 0, 0));
        var definition = AssemblyDefinition.CreateAssembly(name, "OTAPI.Runtime.dll", ModuleKind.Dll);
        var module = definition.MainModule;
        var types = new Dictionary<string, TypeDefinition>(StringComparer.Ordinal);

        foreach (var node in nodes.Where(n => n.Parent is null))
            DefineTree(module, null, node, types);

        foreach (var node in nodes.Where(n => n.IsDelegate && !n.IsHook))
            DefineDelegate(module, types[node.FullName], node, types);
        foreach (var node in nodes.Where(n => n.IsHook))
            DefineDelegate(module, types[node.FullName], node, types);

        foreach (var node in nodes.Where(n => !n.IsDelegate))
            DefineEvents(module, types[node.FullName], node, types);

        return definition;
    }

    static void DefineTree(ModuleDefinition module, TypeDefinition? parent, Node node, Dictionary<string, TypeDefinition> types)
    {
        var nested = parent is not null;
        var attrs = nested
            ? CecilTypeAttributes.NestedPublic | CecilTypeAttributes.Sealed | CecilTypeAttributes.Class
            : CecilTypeAttributes.Public | CecilTypeAttributes.Sealed | CecilTypeAttributes.Class;
        if (!node.IsDelegate)
            attrs |= CecilTypeAttributes.Abstract | CecilTypeAttributes.BeforeFieldInit;

        var type = new TypeDefinition(
            nested ? "" : node.Namespace,
            node.Name,
            attrs,
            node.IsDelegate ? module.ImportReference(typeof(MulticastDelegate)) : module.TypeSystem.Object);
        types[node.FullName] = type;
        if (nested)
            parent!.NestedTypes.Add(type);
        else
            module.Types.Add(type);

        foreach (var child in nodes.Where(n => n.Parent == node.FullName))
            DefineTree(module, type, child, types);
    }

    static void DefineDelegate(ModuleDefinition module, TypeDefinition type, Node node, Dictionary<string, TypeDefinition> types)
    {
        var target = ResolveTarget(node)
            ?? throw new InvalidOperationException($"OTAPI.Runtime cannot resolve {node.FullName}");
        var origParams = OrigParams(target);

        var ctor = new MethodDefinition(
            ".ctor",
            CecilMethodAttributes.Public | CecilMethodAttributes.HideBySig | CecilMethodAttributes.SpecialName | CecilMethodAttributes.RTSpecialName,
            module.TypeSystem.Void);
        ctor.Parameters.Add(new ParameterDefinition(module.ImportReference(typeof(object))));
        ctor.Parameters.Add(new ParameterDefinition(module.ImportReference(typeof(IntPtr))));
        ctor.ImplAttributes = CecilMethodImplAttributes.Runtime | CecilMethodImplAttributes.Managed;
        type.Methods.Add(ctor);

        var invoke = new MethodDefinition(
            "Invoke",
            CecilMethodAttributes.Public | CecilMethodAttributes.HideBySig | CecilMethodAttributes.NewSlot | CecilMethodAttributes.Virtual,
            module.ImportReference(target.ReturnType));
        invoke.HasThis = true;
        invoke.ImplAttributes = CecilMethodImplAttributes.Runtime | CecilMethodImplAttributes.Managed;
        if (node.IsHook)
            invoke.Parameters.Add(new ParameterDefinition(types[node.Parent + "/orig_" + node.MethodName]));
        foreach (var parameter in origParams)
            invoke.Parameters.Add(new ParameterDefinition(module.ImportReference(parameter)));
        type.Methods.Add(invoke);
    }

    static void DefineEvents(ModuleDefinition module, TypeDefinition type, Node node, Dictionary<string, TypeDefinition> types)
    {
        var addRef = module.ImportReference(typeof(OtapiRuntimeBinder).GetMethod(nameof(OtapiRuntimeBinder.Add)));
        var removeRef = module.ImportReference(typeof(OtapiRuntimeBinder).GetMethod(nameof(OtapiRuntimeBinder.Remove)));
        var idPrefix = ToClrName(StripOn(node.FullName)) + "::";

        foreach (var hook in nodes.Where(n => n.Parent == node.FullName && n.IsHook))
        {
            var hookType = types[hook.FullName];
            var id = idPrefix + hook.MethodName;
            var add = DefineAccessor(module, "add_" + hook.MethodName, hookType, addRef, id);
            var remove = DefineAccessor(module, "remove_" + hook.MethodName, hookType, removeRef, id);
            type.Methods.Add(add);
            type.Methods.Add(remove);
            var evt = new EventDefinition(hook.MethodName, CecilEventAttributes.None, hookType)
            {
                AddMethod = add,
                RemoveMethod = remove,
            };
            type.Events.Add(evt);
        }
    }

    static MethodDefinition DefineAccessor(ModuleDefinition module, string name, TypeDefinition hookType, MethodReference binder, string id)
    {
        var method = new MethodDefinition(
            name,
            CecilMethodAttributes.Public | CecilMethodAttributes.Static | CecilMethodAttributes.HideBySig | CecilMethodAttributes.SpecialName,
            module.TypeSystem.Void);
        method.Parameters.Add(new ParameterDefinition(hookType));
        var il = method.Body.GetILProcessor();
        il.Emit(OpCodes.Ldstr, id);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, binder);
        il.Emit(OpCodes.Ret);
        return method;
    }

    static MethodInfo? ResolveTarget(Node node)
    {
        if (node.Parent is null)
            return null;
        var owner = ToClrName(StripOn(node.Parent));
        foreach (var loaded in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = loaded.GetType(owner);
            if (type is null)
                continue;
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            return type.GetMethods(flags).FirstOrDefault(m => m.Name == node.MethodName)
                ?? type.GetMethod(node.MethodName, flags | BindingFlags.FlattenHierarchy);
        }

        return null;
    }

    static Type[] OrigParams(MethodInfo method)
    {
        var args = method.GetParameters().Select(p => p.ParameterType);
        return method.IsStatic ? args.ToArray() : [method.DeclaringType!, .. args];
    }

    static string StripOn(string fullName) =>
        fullName.StartsWith("On.", StringComparison.Ordinal) ? fullName[3..] : fullName;

    static string ToClrName(string cecilName) => cecilName.Replace('/', '+');

    sealed class Node
    {
        public required string FullName;
        public required string Name;
        public required string Namespace;
        public string? Parent;
        public bool IsDelegate;
        public bool IsHook;
        public string MethodName = "";
    }
}
