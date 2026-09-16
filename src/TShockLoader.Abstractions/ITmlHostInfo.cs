namespace TShockLoader.Abstractions;

public interface ITmlHostInfo
{
    string Product { get; }
    string ContractVersion { get; }
    string LoaderVersion { get; }
    string TmlVersion { get; }
}
