using Dalamud.Interface.Windowing;
using System;

namespace FakeName.Windows;

internal class WindowManager : IDisposable
{
    internal readonly WindowSystem WindowSystem = new("FakeName");
    internal FakeNameConfigWindow ConfigWindow { get; }

    public WindowManager()
    {
        ConfigWindow = new FakeNameConfigWindow();
        WindowSystem.AddWindow(ConfigWindow);

        Service.Interface.UiBuilder.Draw += DrawUi;
        Service.Interface.UiBuilder.OpenConfigUi += ConfigWindow.Toggle;
        Service.Interface.UiBuilder.OpenMainUi += ConfigWindow.Toggle;
    }

    public void Dispose()
    {
        Service.Interface.UiBuilder.Draw -= DrawUi;
        Service.Interface.UiBuilder.OpenConfigUi -= ConfigWindow.Toggle;
        Service.Interface.UiBuilder.OpenMainUi -= ConfigWindow.Toggle;
        WindowSystem.RemoveAllWindows();
    }

    private void DrawUi()
    {
        WindowSystem.Draw();
    }
}
