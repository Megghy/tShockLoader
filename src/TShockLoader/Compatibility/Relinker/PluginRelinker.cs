using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using TerrariaApi.Server;

namespace tShockLoader.Relinker;

public sealed class RelinkUnsupportedException : InvalidOperationException
{
    public RelinkUnsupportedException(string pluginId, IReadOnlyList<string> types)
        : base($"Relinker cannot map OTAPI.Runtime IL hooks in {pluginId}: {string.Join(", ", types)}")
    {
    }
}

public static class PluginRelinker
{
    const string TmlAssembly = "tModLoader";
    const string FnaAssembly = "FNA";
    const string ApiAssembly = "TerrariaApi.Server";
    const string TShockAssembly = "TShockAPI";

    public static bool NeedsRelink(byte[] pe)
    {
        using var input = new MemoryStream(pe, writable: false);
        var module = ModuleDefinition.ReadModule(input, new ReaderParameters
        {
            InMemory = true,
            ReadingMode = ReadingMode.Immediate,
        });

        foreach (var reference in module.AssemblyReferences)
        {
            if (IsKnownSource(reference.Name))
                return true;
        }

        return false;
    }

    public static byte[] Relink(byte[] pe, string pluginId)
    {
        using var input = new MemoryStream(pe, writable: false);
        var module = ModuleDefinition.ReadModule(input, new ReaderParameters
        {
            InMemory = true,
            ReadingMode = ReadingMode.Immediate,
        });

        var tml = GetOrAddRef(module, TmlAssembly);
        var fna = GetOrAddRef(module, FnaAssembly);
        var api = GetOrAddRef(module, ApiAssembly);
        var tshock = GetOrAddRef(module, TShockAssembly);
        var unsupported = new List<string>();

        foreach (var typeRef in module.GetTypeReferences())
            ConsiderRuntime(typeRef, unsupported);
        foreach (var member in module.GetMemberReferences())
            ConsiderRuntime(member.DeclaringType, unsupported);

        if (unsupported.Count > 0)
            throw new RelinkUnsupportedException(pluginId, unsupported);

        foreach (var typeRef in module.GetTypeReferences())
        {
            if (typeRef.Scope is not AssemblyNameReference scope || !IsKnownSource(scope.Name))
                continue;
            if (scope.Name == "OTAPI.Runtime")
                continue;

            if (RewriteTileType(typeRef, api))
                continue;

            var target = Map(typeRef);
            if (target is null || scope.Name.Equals(target, StringComparison.Ordinal))
                continue;

            typeRef.Scope = target switch
            {
                TmlAssembly => tml,
                FnaAssembly => fna,
                TShockAssembly => tshock,
                _ => api,
            };
        }

        ApplyKnownMembers(module);

        using var output = new MemoryStream();
        module.Write(output);
        return output.ToArray();
    }

    static bool IsKnownSource(string name) => name is
        "OTAPI" or "OTAPI.Runtime" or "OTAPI.Upcoming" or
        "TerrariaServer" or "Terraria" or "ModFramework" or
        "Microsoft.Xna.Framework" or "Microsoft.Xna.Framework.Game" or
        "Microsoft.Xna.Framework.Graphics" or "Microsoft.Xna.Framework.Xact";

    static bool RewriteTileType(TypeReference type, AssemblyNameReference api)
    {
        var ns = type.Namespace ?? "";
        if (type.Name == "ITile" && ns is "Terraria" or "OTAPI.Tile")
        {
            type.Namespace = "Terraria";
            type.Scope = api;
            return true;
        }
        if (type.Name == "Tile" && ns == "Terraria")
        {
            type.Name = "TileRef";
            type.Namespace = "TerrariaApi.Server";
            type.Scope = api;
            return true;
        }
        if (type.Name is "ITileCollection" or "TileCollection")
        {
            type.Name = "ITileCollection";
            type.Namespace = "OTAPI.Tile";
            type.Scope = api;
            return true;
        }
        if (type.Name == "ICollection`1" && ns == "ModFramework")
        {
            type.Scope = api;
            return true;
        }
        return false;
    }

    static void ConsiderRuntime(TypeReference? type, List<string> unsupported)
    {
        if (type is null || type.Scope is not AssemblyNameReference { Name: "OTAPI.Runtime" })
            return;
        var ns = type.Namespace ?? "";
        if (ns is "IL" || ns.StartsWith("IL.", StringComparison.Ordinal))
        {
            unsupported.Add($"{ns}.{type.Name}");
            return;
        }

        OtapiRuntime.Request(type);
    }

    static string? Map(TypeReference type)
    {
        var ns = type.Namespace ?? "";
        if (ns is "ModFramework" || ns.StartsWith("ModFramework.", StringComparison.Ordinal))
            return ApiAssembly;
        if (ns.StartsWith("Microsoft.Xna.Framework", StringComparison.Ordinal))
            return FnaAssembly;
        if (ns is "TShockAPI" || ns.StartsWith("TShockAPI.", StringComparison.Ordinal) || ns is "Rests")
            return TShockAssembly;
        if (ns is "TerrariaApi" || ns.StartsWith("TerrariaApi.", StringComparison.Ordinal) ||
            ns is "OTAPI" || ns.StartsWith("OTAPI.", StringComparison.Ordinal) ||
            ns.Length == 0 && type.Name == "PacketTypes" ||
            ns == "System.IO.Streams" && type.Name == "StreamExt")
            return ApiAssembly;
        if (ns is "Terraria" || ns.StartsWith("Terraria.", StringComparison.Ordinal))
            return TmlAssembly;
        if (ns.Length == 0 && type.Scope is AssemblyNameReference scope &&
            scope.Name is "OTAPI" or "TerrariaServer" or "TerrariaApi.Server")
            return ApiAssembly;
        return null;
    }

    static readonly HashSet<string> MainIntToDoubleFields =
    [
        "dayRate",
        "desiredWorldTilesUpdateRate",
        "rainTime",
        "ladyBugRainBoost",
    ];

    static readonly string[] TileDataFields =
    [
        "type", "wall", "liquid",
        "sTileHeader", "bTileHeader", "bTileHeader2", "bTileHeader3",
        "frameX", "frameY",
    ];

    static void ApplyKnownMembers(ModuleDefinition module)
    {
        var widen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var member in module.GetMemberReferences())
        {
            if (member is not FieldReference field || !IsMainIntToDouble(field))
                continue;
            widen.Add(field.Name);
            field.FieldType = module.TypeSystem.Double;
        }

        var tilesProp = typeof(TileAdapter).GetProperty(nameof(TileAdapter.Tiles))!;
        var tilesGet = module.ImportReference(tilesProp.GetMethod);
        var tilesSet = module.ImportReference(tilesProp.SetMethod);
        var tileAccessors = new Dictionary<string, (MethodReference Get, MethodReference Set)>(StringComparer.Ordinal);
        foreach (var name in TileDataFields)
        {
            var prop = typeof(TileRef).GetProperty(name, BindingFlags.Public | BindingFlags.Instance)!;
            tileAccessors[name] = (module.ImportReference(prop.GetMethod!), module.ImportReference(prop.SetMethod!));
        }

        foreach (var type in module.GetTypes())
        foreach (var method in type.Methods)
        {
            if (!method.HasBody)
                continue;

            var il = method.Body.GetILProcessor();
            for (var i = 0; i < method.Body.Instructions.Count; i++)
            {
                var inst = method.Body.Instructions[i];
                if (inst.OpCode == OpCodes.Ldstr && inst.Operand is "SQLite.MS")
                    inst.Operand = "SQLite";

                if (inst.OpCode == OpCodes.Ldstr && inst.Operand is "免禁指令")
                {
                    var call = inst.Next;
                    var br = call?.Next;
                    var skip = br?.Next;
                    if (call is not null
                        && call.OpCode == OpCodes.Callvirt
                        && br is not null
                        && (br.OpCode == OpCodes.Brfalse || br.OpCode == OpCodes.Brfalse_S)
                        && skip is not null
                        && skip.OpCode == OpCodes.Ret)
                    {
                        skip.OpCode = OpCodes.Nop;
                    }
                }

                if (inst.Operand is not FieldReference field)
                    continue;

                if (field.Name == "SQLiteMS" && field.DeclaringType.Name == "ProviderName")
                {
                    inst.Operand = new FieldReference("SQLite", field.FieldType, field.DeclaringType);
                    continue;
                }

                if (field.Name == "tile" && field.DeclaringType.FullName == "Terraria.Main")
                {
                    if (inst.OpCode == OpCodes.Ldsfld)
                    {
                        inst.OpCode = OpCodes.Call;
                        inst.Operand = tilesGet;
                    }
                    else if (inst.OpCode == OpCodes.Stsfld)
                    {
                        inst.OpCode = OpCodes.Call;
                        inst.Operand = tilesSet;
                    }
                    continue;
                }

                if (field.DeclaringType.Name == "TileRef" && tileAccessors.TryGetValue(field.Name, out var accessors))
                {
                    if (inst.OpCode == OpCodes.Ldfld)
                    {
                        inst.OpCode = OpCodes.Callvirt;
                        inst.Operand = accessors.Get;
                    }
                    else if (inst.OpCode == OpCodes.Stfld)
                    {
                        inst.OpCode = OpCodes.Callvirt;
                        inst.Operand = accessors.Set;
                    }
                    continue;
                }

                if (IsMainIntToDouble(field))
                {
                    widen.Add(field.Name);
                    field.FieldType = module.TypeSystem.Double;
                }

                if (!widen.Contains(field.Name) || field.DeclaringType.FullName != "Terraria.Main")
                    continue;

                if (inst.OpCode == OpCodes.Ldsfld || inst.OpCode == OpCodes.Ldfld)
                {
                    il.InsertAfter(inst, il.Create(OpCodes.Conv_I4));
                    i++;
                }
                else if (inst.OpCode == OpCodes.Stsfld || inst.OpCode == OpCodes.Stfld)
                {
                    il.InsertBefore(inst, il.Create(OpCodes.Conv_R8));
                    i++;
                }
            }
        }
    }

    static bool IsMainIntToDouble(FieldReference field) =>
        field.DeclaringType.FullName == "Terraria.Main" &&
        MainIntToDoubleFields.Contains(field.Name) &&
        field.FieldType.MetadataType == MetadataType.Int32;

    static AssemblyNameReference GetOrAddRef(ModuleDefinition module, string name)
    {
        foreach (var existing in module.AssemblyReferences)
        {
            if (existing.Name == name)
                return existing;
        }

        var added = new AssemblyNameReference(name, new Version(0, 0, 0, 0));
        module.AssemblyReferences.Add(added);
        return added;
    }
}
