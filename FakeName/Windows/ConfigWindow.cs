using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System.Linq;

namespace FakeName.Windows;

internal class ConfigWindow : Window
{
    public ConfigWindow() : base("Fake Name")
    {
    }

    public void Open() => IsOpen = true;

    public override void Draw()
    {
        var localPlayer = Service.ClientState.LocalPlayer;
        var localName = "";
        if (localPlayer != null)
        {
            localName = localPlayer.Name.TextValue;
        }

        if (ImGui.Checkbox("Enable", ref Service.Config.enabled))
        {
            Service.Config.SaveConfig();
        }

        if (ImGui.Checkbox("Only Change in Stream", ref Service.Config.OnlyInStream))
        {
            Service.Config.SaveConfig();
        }

        if (ImGui.Checkbox("Change All Player's Name", ref Service.Config.AllPlayerReplace))
        {
            Service.Config.SaveConfig();
        }

        if (ImGui.InputText("Character Name", ref Service.Config.FakeNameText, 100))
        {
            Service.Config.SaveConfig();
        }

        if (ImGui.Button("Reset"))
        {
            Service.Config.FakeNameText = localName;
            Service.Config.SaveConfig();
        }

        ImGui.Separator();

        if (!Service.Config.NameDict.Any(p => string.IsNullOrEmpty(p.Item1)))
        {
            Service.Config.NameDict.Add((string.Empty, string.Empty));
        }

        if (ImGui.BeginTable("Name Dict things", 3, ImGuiTableFlags.Borders
            | ImGuiTableFlags.Resizable
            | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableNextRow(ImGuiTableRowFlags.Headers);

            ImGui.TableNextColumn();
            ImGui.TableHeader("Original Name");

            ImGui.TableNextColumn();
            ImGui.TableHeader("Replaced Name");

            ImGui.TableNextColumn();
            ImGui.TableHeader("Delete");

            var index = 0;

            var removeIndex = -1;
            var changedIndex = -1;

            var changedValue = (string.Empty, string.Empty);
            foreach ((var key, var value) in Service.Config.NameDict)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                var str = key;
                if(ImGui.InputTextWithHint($"##NameDict Key{index}", "Original Name", ref str, 1024))
                {
                    changedIndex = index;
                    changedValue = (str, value);
                }
                ImGui.TableNextColumn();

                str = value;

                if(ImGui.InputTextWithHint($"##NameDict Value{index}", "Replace Name", ref str, 1024))
                {
                    changedIndex = index;
                    changedValue = (key, str);
                }
                ImGui.TableNextColumn();

                ImGui.PushFont(UiBuilder.IconFont);
                var result = ImGui.Button(FontAwesomeIcon.Ban.ToIconString() + $"##Remove NameDict Key{index}");
                ImGui.PopFont();

                if (result)
                {
                    removeIndex = index;
                }

                index++;
            }

            ImGui.EndTable();
            if (removeIndex > -1)
            {
                Service.Config.NameDict.RemoveAt(removeIndex);
                Service.Config.SaveConfig();
            }
            if (changedIndex > -1)
            {
                Service.Config.NameDict[changedIndex] = changedValue;
                Service.Config.SaveConfig();
            }
        }
    }
}
