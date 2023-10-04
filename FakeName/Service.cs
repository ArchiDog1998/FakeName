using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace FakeName;

internal class Service
{
    internal static Configuration Config { get; set; }

    [PluginService]
    internal static DalamudPluginInterface Interface { get; private set; }

    [PluginService]
    internal static IClientState ClientState { get; private set; }

    [PluginService]
    internal static ICommandManager CommandManager { get; private set; }

    [PluginService]
    internal static IObjectTable ObjectTable { get; private set; }
    [PluginService]
    public static IFramework Framework  { get; private set; }

    [PluginService]
    public static ICondition Condition { get; private set; }


    [PluginService]
    public static IGameGui GameGui { get; private set; }

    [PluginService]
    public static IGameInteropProvider Hook { get; private set; }

    [PluginService]
    public static IPluginLog Log { get; private set; }
}
