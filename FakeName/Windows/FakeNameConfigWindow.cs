using Dalamud.Interface;
using Dalamud.Interface.Internal;
using ImGuiNET;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using XIVConfigUI;
using XIVConfigUI.SearchableConfigs;

namespace FakeName.Windows;

internal class FakeNameConfigWindow() : ConfigWindow(typeof(FakeNameConfigWindow).Assembly.GetName())
{
    public class Config : SearchableConfig
    {
        public override void AfterConfigChange(Searchable item)
        {
            Service.Config.SaveConfig();
        }
    }

    [Description("Setting")]
    public class SettingItem : ConfigWindowItem
    {
        public override void Draw(ConfigWindow window)
        {
            window.Collection.DrawItems(0);
        }

        public override bool GetIcon(out IDalamudTextureWrap texture)
        {
            return ImageLoader.GetTexture(14, out texture);
        }
    }

    [Description("Player Names")]
    public class NameItem : ConfigWindowItem
    {
        public override bool IsSkip => !Service.Config.enabled;

        public override void Draw(ConfigWindow window)
        {
            DrawList(Service.Config.NameDict);
        }

        public override bool GetIcon(out IDalamudTextureWrap texture)
        {
            return ImageLoader.GetTexture(43, out texture);
        }
    }

    [Description("FC Names")]
    public class FCNameItem : ConfigWindowItem
    {
        public override bool IsSkip => !Service.Config.enabled || !Service.Config.FCNameReplace;

        public override void Draw(ConfigWindow window)
        {
            DrawList(Service.Config.FCNameDict);
        }

        public override bool GetIcon(out IDalamudTextureWrap texture)
        {
            return ImageLoader.GetTexture(8, out texture);
        }
    }

    protected override string Kofi => "B0B0IN5DX";
    protected override string Crowdin => "fakename";
    protected override string DiscordServerInviteLink => "9D4E8eZW5g";
    protected override string DiscordServerID => "1228953752585637908";

    public override SearchableCollection Collection { get; } = new(Service.Config, new Config());

    private static void DrawList(List<(string, string)> data)
    {

        if (!data.Any(p => string.IsNullOrEmpty(p.Item1)))
        {
            data.Add((string.Empty, string.Empty));
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
            foreach ((var key, var value) in data)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                var str = key;
                if (ImGui.InputTextWithHint($"##NameDict Key{index}", "Original Name", ref str, 1024))
                {
                    changedIndex = index;
                    changedValue = (str, value);
                }
                ImGui.TableNextColumn();

                str = value;

                if (ImGui.InputTextWithHint($"##NameDict Value{index}", "Replace Name", ref str, 1024))
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
                data.RemoveAt(removeIndex);
                Service.Config.SaveConfig();
            }
            if (changedIndex > -1)
            {
                data[changedIndex] = changedValue;
                Service.Config.SaveConfig();
            }
        }
    }
}
