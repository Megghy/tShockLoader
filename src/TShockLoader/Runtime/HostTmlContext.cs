using Terraria.ModLoader;
using TShockLoader.Abstractions;

namespace tShockLoader.Runtime;

sealed class HostTmlContext : ITmlContext
{
    public ITmlHostInfo Host { get; } = new Info();

    sealed class Info : ITmlHostInfo
    {
        public string Product => "tShockLoader";
        public string ContractVersion => "1.0.0";
        public string LoaderVersion => "0.1.0";
        public string TmlVersion => BuildInfo.tMLVersion.ToString();
    }
}
