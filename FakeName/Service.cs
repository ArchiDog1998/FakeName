using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace FakeName;

internal class Service
{
    internal static Configuration Config { get; set; } = null!;

    [PluginService]
    internal static DalamudPluginInterface Interface { get; private set; } = null!;

    [PluginService]
    internal static IClientState ClientState { get; private set; } = null!;

    [PluginService]
    internal static IObjectTable ObjectTable { get; private set; } = null!;

    [PluginService]
    public static IFramework Framework  { get; private set; } = null!;

    [PluginService]
    public static ICondition Condition { get; private set; } = null!;

    [PluginService]
    public static IGameGui GameGui { get; private set; } = null!;

    [PluginService]
    public static IGameInteropProvider Hook { get; private set; } = null!;

    [PluginService]
    public static IPluginLog Log { get; private set; } = null!;
}
