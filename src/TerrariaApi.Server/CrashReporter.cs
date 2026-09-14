namespace TerrariaApi.Reporting;

public sealed class CrashReporter
{
    public static event EventHandler? HeapshotRequesting;

    internal static void RequestHeapshot() => HeapshotRequesting?.Invoke(null, EventArgs.Empty);
}
