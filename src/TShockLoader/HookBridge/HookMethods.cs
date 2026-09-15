using System.Reflection;

namespace TerrariaApi.Server.Hooking;

static class HookMethods
{
    internal static MethodInfo Require(Type type, string name, BindingFlags flags, params Type[] parameters)
    {
        var method = parameters.Length == 0
            ? type.GetMethod(name, flags)
            : type.GetMethod(name, flags, parameters);
        if (method is null)
            throw new MissingMethodException(type.FullName, name);
        return method;
    }
}
