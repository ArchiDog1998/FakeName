using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
namespace FakeName;

public class Hooker
{
    private delegate void AtkTextNodeSetTextDelegate(IntPtr node, IntPtr text);

    /// <summary>
    /// https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Component/GUI/AtkTextNode.cs#L79
    /// </summary>
    [Signature("E8 ?? ?? ?? ?? 8D 4E 32", DetourName = nameof(AtkTextNodeSetTextDetour))]
    private Hook<AtkTextNodeSetTextDelegate> AtkTextNodeSetTextHook { get; init; }

    private delegate void SetNamePlateDelegate(IntPtr addon, bool isPrefixTitle, 
        bool displayTitle, IntPtr titlePtr, IntPtr namePtr, IntPtr fcNamePtr, int iconId);

    /// <summary>
    /// https://github.com/Haplo064/JobIcons/blob/master/PluginAddressResolver.cs#L34
    /// </summary>
    [Signature("48 89 5C 24 ?? 48 89 6C 24 ?? 56 57 41 54 41 56 41 57 48 83 EC 40 44 0F B6 E2", DetourName = nameof(SetNamePlateDetour))]
    private Hook<SetNamePlateDelegate> SetNamePlateHook { get; init; }

    public static Dictionary<string[], string> Replacement { get; } = new Dictionary<string[], string>();

    internal unsafe Hooker()
    {
        SignatureHelper.Initialise(this);

        AtkTextNodeSetTextHook.Enable();
        SetNamePlateHook.Enable();

        Service.Framework.Update += Framework_Update;
        Service.ClientState.Login += ClientState_Login;
    }

    public unsafe void Dispose()
    {
        AtkTextNodeSetTextHook.Dispose();
        SetNamePlateHook.Dispose();
        Service.Framework.Update -= Framework_Update;
        Service.ClientState.Login -= ClientState_Login;
    }

    private void ClientState_Login(object sender, EventArgs e)
    {
        var player = Service.ClientState.LocalPlayer;
        if (player == null) return;

        if (!Service.Config.CharacterNames.Contains(player.Name.TextValue))
        {
            Service.Config.CharacterNames.Add(player.Name.TextValue);
            Service.Config.SaveConfig();
        }
    }

    private unsafe void Framework_Update(Dalamud.Game.Framework framework)
    {
        if (!Service.Condition.Any()) return;
        var player = Service.ClientState.LocalPlayer;
        if (player == null) return;

        Replacement.Clear();
        Replacement[GetNamesSimple(player.Name.TextValue)] = Service.Config.FakeNameText;

        if (!Service.Config.AllPlayerReplace) return;

        foreach (var obj in Service.ObjectTable)
        {
            if (obj is not PlayerCharacter member) continue;
            var memberName = member.Name.TextValue;
            if (memberName == player.Name.TextValue) continue;

            Replacement[new string[] { memberName }] = GetChangedName(memberName);
        }

        foreach (var obj in Service.Config.FriendList)
        {
            Replacement[new string[] { obj }] = GetChangedName(obj);
        }

        var friendList = (AddonFriendList*)Service.GameGui.GetAddonByName("FriendList", 1);
        if (friendList == null) return;

        var list = friendList->FriendList;
        for (int i = 0; i < list->ListLength; i++)
        {
            var item = list->ItemRendererList[i];
            var textNode = item.AtkComponentListItemRenderer->AtkComponentButton.ButtonTextNode;

            if (Service.Config.FriendList.Add(textNode->NodeText.ToString()))
            {
                Service.Config.SaveConfig();
            }
        }
    }

    private static string[] GetNamesSimple(string name)
    {
        var names = name.Split(' ');
        if (names.Length != 2) return new string[] { name };

        var first = names[0];

        return new string[]
        {
            name,
            first,
        };
    }

    private void AtkTextNodeSetTextDetour(IntPtr node, IntPtr text)
    {
        if (!Service.Config.Enabled)
        {
            AtkTextNodeSetTextHook.Original(node,text);
            return;
        }
        AtkTextNodeSetTextHook.Original(node, ChangeName(text));
    }

    private unsafe void SetNamePlateDetour(IntPtr namePlateObjectPtr, bool isPrefixTitle,
        bool displayTitle, IntPtr titlePtr, IntPtr namePtr, IntPtr fcNamePtr, int iconId)
    {
        try
        {
            if (Service.Config.Enabled)
            {
                var nameSe = GetSeStringFromPtr(namePtr).TextValue;
                if (Service.ClientState.LocalPlayer != null && GetNamesFull(Service.ClientState.LocalPlayer.Name.TextValue).Contains(nameSe))
                {
                    GetPtrFromSeString(Service.Config.FakeNameText, namePtr);
                }
                else if (Service.Config.AllPlayerReplace)
                {
                    GetPtrFromSeString(GetChangedName(nameSe), namePtr);
                }
            }
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "Failed to change name plate");
        }

        SetNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle,
            titlePtr, namePtr, fcNamePtr, iconId);
    }


    private static string[] GetNamesFull(string name)
    {
        var names = name.Split(' ');
        if (names.Length != 2) return new string[] { name };

        var first = names[0];
        var last = names[1];
        var firstShort = first.ToUpper()[0] + ".";
        var lastShort = last.ToUpper()[0] + ".";

        return new string[]
        {
            name,
            $"{first} {lastShort}",
            $"{firstShort} {last}",
            $"{firstShort} {lastShort}",
            first, last,
        };
    }

    public static IntPtr ChangeName(IntPtr seStringPtr)
    {
        if (seStringPtr == IntPtr.Zero) return seStringPtr;

        try
        {
            var str = GetSeStringFromPtr(seStringPtr);
            if (ChangeSeString(str))
            {
                GetPtrFromSeString(str, seStringPtr);
            }
            return seStringPtr;
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "Something wrong with change name!");
            return seStringPtr;
        }
    }

    public static void GetPtrFromSeString(SeString str, IntPtr ptr)
    {
        var bytes = str.Encode();
        Marshal.Copy(bytes, 0, ptr, bytes.Length);
        Marshal.WriteByte(ptr, bytes.Length, 0);
    }

    public static SeString GetSeStringFromPtr(IntPtr seStringPtr)
    {
        var offset = 0;
        unsafe
        {
            while (*(byte*)(seStringPtr + offset) != 0)
                offset++;
        }
        var bytes = new byte[offset];
        Marshal.Copy(seStringPtr, bytes, 0, offset);
        return SeString.Parse(bytes);
    }

    public static bool ChangeSeString(SeString seString)
    {
        try
        {
            if (seString.Payloads.All(payload => payload.Type != PayloadType.RawText)) return false;

            return Replacement.Any(pair => ReplacePlayerName(seString, pair.Key, pair.Value))
                || ReplacePlayerName(seString, Service.Config.CharacterNames, Service.Config.FakeNameText);
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "Something wrong with replacement!");
            return false;
        }
    }

    public static string GetChangedName(string str)
    {
        if (string.IsNullOrEmpty(str)) return str;
        var lt = str.Split(' ');
        if (lt.Length != 2) return str;
        return string.Join(" . ", lt.Select(s => s.ToUpper().FirstOrDefault()));
    }

    private static bool ReplacePlayerName(SeString text, IEnumerable<string> names, string replacement)
    {
        foreach (var name in names)
        {
            if (ReplacePlayerName(text, name, replacement))
            {
                return true;
            }
        }
        return false;
    }

    private static bool ReplacePlayerName(SeString text, string name, string replacement)
    {
        if (string.IsNullOrEmpty(name)) return false;
        if (text.Payloads.Count > 10) return false;

        var result = false;
        foreach (var payLoad in text.Payloads)
        {
            if (payLoad is TextPayload load)
            {
                if (string.IsNullOrEmpty(load.Text)) continue;

                var t = load.Text.Replace(name, replacement);
                if (t == load.Text) continue;
                load.Text = t;
                result = true;
            }
        }
        return result;
    }
}
