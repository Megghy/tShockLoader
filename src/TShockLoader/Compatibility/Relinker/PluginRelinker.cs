using Mono.Cecil;

namespace tShockLoader.Relinker;

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
            if (reference.Name is "OTAPI" or "OTAPI.Runtime" or "OTAPI.Upcoming"
                or "TerrariaServer" or "TerrariaApi.Server" or "Microsoft.Xna.Framework" or "Terraria")
                return true;
        }

        return false;
    }

    public static byte[] Relink(byte[] pe)
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

        foreach (var typeRef in module.GetTypeReferences())
        {
            if (typeRef.Scope is not AssemblyNameReference scope)
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

        using var output = new MemoryStream();
        module.Write(output);
        return output.ToArray();
    }

    static string? Map(TypeReference type)
    {
        var ns = type.Namespace ?? "";
        if (type.Name == "ITile" && (ns is "Terraria" or "OTAPI.Tile"))
            return ApiAssembly;
        if (ns is "OTAPI" || ns.StartsWith("OTAPI.", StringComparison.Ordinal) ||
            ns is "TerrariaApi" || ns.StartsWith("TerrariaApi.", StringComparison.Ordinal))
            return ApiAssembly;
        if (ns is "TShockAPI" || ns.StartsWith("TShockAPI.", StringComparison.Ordinal) || ns is "Rests")
            return TShockAssembly;
        if (ns.StartsWith("Microsoft.Xna.Framework", StringComparison.Ordinal))
            return FnaAssembly;
        if (ns is "Terraria" || ns.StartsWith("Terraria.", StringComparison.Ordinal))
            return TmlAssembly;
        if (ns.Length == 0 && type.Scope is AssemblyNameReference scope &&
            scope.Name is "OTAPI" or "TerrariaServer" or "TerrariaApi.Server")
            return ApiAssembly;
        return null;
    }

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
