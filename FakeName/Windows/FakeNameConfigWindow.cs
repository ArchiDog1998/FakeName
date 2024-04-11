using Dalamud.Interface;
using Dalamud.Interface.Internal;
using ImGuiNET;
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

    public class SettingItem : ConfigWindowItem
    {
        public override void Draw(ConfigWindow window)
        {
            window.Collection.DrawItems(0);

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

        public override bool GetIcon(out IDalamudTextureWrap texture)
        {
            return ImageLoader.GetTexture(14, out texture);
        }
    }

    public override SearchableCollection Collection { get; } = new(Service.Config, new Config());
}
