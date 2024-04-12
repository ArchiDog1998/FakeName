using Dalamud.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using XIVConfigUI;

namespace FakeName;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    [JsonIgnore]
    public bool Enabled
    {
        get
        {
            if (!enabled) return false;

            if (OnlyInStream)
            {
                return Hooker.IsStreaming;
            }
            return true;
        }
    }

    [UI("Enable")]
    public bool enabled { get; set; } = false;

    [UI("Only Change in Stream", Parent = nameof(enabled))]
    public bool OnlyInStream { get; set; } = true;

    [UI("Change All Player's Name", Parent = nameof(enabled))]
    public bool AllPlayerReplace { get; set; } = false;

    [UI("Character Name", Parent = nameof(enabled))]
    public string FakeNameText { get; set; } = Service.ClientState.LocalPlayer?.Name.TextValue ?? string.Empty;

    [UI("Change FC Names", Parent = nameof(enabled))]
    public bool FCNameReplace { get; set; } = true;

    public HashSet<string> CharacterNames = [];
    public HashSet<string> FriendList = [];

    public List<(string, string)> NameDict = [];
    public List<(string, string)> FCNameDict = [];
    internal void SaveConfig()
    {
        Service.Interface.SavePluginConfig(this);
    }
}
