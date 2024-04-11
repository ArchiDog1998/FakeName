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
            if(OnlyInStream)
            {
                return Hooker.IsStreaming;
            }
            else
            {
                return enabled;
            }
        }
    }

    [UI("Enable")]
    public bool enabled { get; set; } = false;

    [UI("Only Change in Stream")]
    public bool OnlyInStream { get; set; } = true;

    [UI("Change All Player's Name")]
    public bool AllPlayerReplace { get; set; } = false;

    [UI("Character Name")]
    public string FakeNameText { get; set; } = Service.ClientState.LocalPlayer?.Name.TextValue ?? string.Empty;

    public HashSet<string> CharacterNames = [];
    public HashSet<string> FriendList = [];

    public List<(string, string)> NameDict = [];
    internal void SaveConfig()
    {
        Service.Interface.SavePluginConfig(this);
    }
}
