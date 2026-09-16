namespace TShockLoader.Abstractions;

public static class TmlBridge
{
    static ITmlContext? current;

    public static bool TryGet(out ITmlContext context)
    {
        var value = current;
        context = value!;
        return value is not null;
    }

    internal static void Bind(ITmlContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (current is not null)
            throw new InvalidOperationException("TmlBridge is already bound for this session.");
        current = context;
    }

    internal static void Unbind() => current = null;
}
