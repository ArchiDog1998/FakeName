using Dalamud.Plugin;
using FakeName.Windows;
using System;
using XIVConfigUI;

namespace FakeName;

public class Plugin : IDalamudPlugin
{
    internal Hooker Hooker { get; }

    internal WindowManager WindowManager { get; }

    public Plugin(DalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();
        Service.Config = Service.Interface.GetPluginConfig() as Configuration ?? new Configuration();

        XIVConfigUIMain.Init(pluginInterface, "/fakename", "Open a config window about fake name.", OnCommand, typeof(Configuration), typeof(UiString));

        WindowManager = new WindowManager();
        Hooker = new Hooker();
    }

    public void Dispose()
    {
        Hooker.Dispose();
        WindowManager.Dispose();

        XIVConfigUIMain.Dispose();
        GC.SuppressFinalize(this);
    }

    private void OnCommand(string arguments)
    {
        WindowManager.ConfigWindow.Toggle();
    }
}
