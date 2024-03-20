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

    public HashSet<string> CharacterNames = [];
    public HashSet<string> FriendList = [];

    public List<(string, string)> NameDict = [];
    internal void SaveConfig()
    {
        Service.Interface.SavePluginConfig(this);
    }
}
