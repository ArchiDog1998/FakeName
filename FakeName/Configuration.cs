using Dalamud.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

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

    public bool enabled = false;
    public bool OnlyInStream = true;

    public bool AllPlayerReplace = false;

    public string FakeNameText = "";

    public HashSet<string> CharacterNames = new();
    public HashSet<string> FriendList = new();

    public List<(string, string)> NameDict = new();
    internal void SaveConfig()
    {
        Service.Interface.SavePluginConfig(this);
    }
}
