using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Gui.NamePlate;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using Svg;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;

namespace FakeName;

public class Hooker
{
    private delegate void AtkTextNodeSetTextDelegate(IntPtr node, IntPtr text);

    /// <summary>
    /// https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Component/GUI/AtkTextNode.cs#L48
    /// </summary>
    [Signature("E8 ?? ?? ?? ?? 8D 4E 32", DetourName = nameof(AtkTextNodeSetTextDetour))]
    private Hook<AtkTextNodeSetTextDelegate>? AtkTextNodeSetTextHook { get; init; }

    public static List<(string[], string)> Replacement { get; private set; } = [];

    internal unsafe Hooker()
    {
        Service.Hook.InitializeFromAttributes(this);

        AtkTextNodeSetTextHook?.Enable();

        Service.NamePlate.OnNamePlateUpdate += NamePlate_OnNamePlateUpdate;

        Service.Framework.Update += Framework_Update;
        Service.ClientState.Login += ClientState_Login;
    }

    private void NamePlate_OnNamePlateUpdate(INamePlateUpdateContext context, IReadOnlyList<INamePlateUpdateHandler> handlers)
    {
        foreach (var handler in handlers)
        {
            switch (handler.NamePlateKind)
            {
                case NamePlateKind.PlayerCharacter:
                    var str = handler.Name?.TextValue;

                    if (!string.IsNullOrEmpty(str))
                    {
                        handler.Name = ReplaceNameplate(str);
                    }

                    var nameSe = handler.FreeCompanyTag.TextValue;
                    foreach ((var key, var value) in Service.Config.FCNameDict)
                    {
                        if (key != nameSe) continue;
                        handler.FreeCompanyTag = value;
                        break;
                    }
                    break;

                case NamePlateKind.EventNpcCompanion:
                    str = handler.Title.ToString();
                    var start = str[0];
                    var end = str[^1];
                    handler.Title = start + ReplaceNameplate(str[1..^1]) + end;
                    break;
            }
        }
    }

    private static string ReplaceNameplate(string str)
    {
        if (Service.ClientState.LocalPlayer != null && GetNamesFull(Service.ClientState.LocalPlayer.Name.TextValue).Contains(str))
        {
            return Service.Config.FakeNameText;
        }
        else if (Service.Config.AllPlayerReplace)
        {
            return GetChangedName(str);
        }
        else
        {
            return str;
        }
    }

    public unsafe void Dispose()
    {
        AtkTextNodeSetTextHook?.Dispose();
        Service.NamePlate.OnNamePlateUpdate -= NamePlate_OnNamePlateUpdate;

        Service.Framework.Update -= Framework_Update;
        Service.ClientState.Login -= ClientState_Login;
    }

    private void ClientState_Login()
    {
        var player = Service.ClientState.LocalPlayer;
        if (player == null) return;

        if (!Service.Config.CharacterNames.Contains(player.Name.TextValue))
        {
            Service.Config.CharacterNames.Add(player.Name.TextValue);
            Service.Config.SaveConfig();
        }
    }

    private static bool IsRunning = false;
    public static bool IsStreaming { get; set; } = false;
    private static DateTime LastCheck = DateTime.MinValue;
    private static readonly string[] AppEqualNames =
    [
        "obs32",
        "obs64",
    ];
    private static readonly string[] AppStartNames =
    [
        "XSplit",
    ];
    private unsafe void Framework_Update(IFramework framework)
    {
        var replacements = new List<(string[], string)>();

        try
        {
            if ((DateTime.Now - LastCheck).TotalSeconds > 1)
            {
                LastCheck = DateTime.Now;

                var processes = Process.GetProcesses();
                IsStreaming = processes.Any(x =>
                AppStartNames.Any(n => x.ProcessName.StartsWith(n, StringComparison.OrdinalIgnoreCase))
                || AppEqualNames.Any(n => x.ProcessName.Equals(n, StringComparison.OrdinalIgnoreCase)));
            }

            var player = Service.ClientState.LocalPlayer;

            if (player != null)
            {
                replacements.Add((GetNamesFull(player.Name.TextValue), Service.Config.FakeNameText));
            }

            foreach ((var key, var value) in Service.Config.NameDict)
            {
                replacements.Add((new string[] { key }, value));
            }

            if (!Service.Config.AllPlayerReplace) return;

            foreach (var obj in Service.ObjectTable)
            {
                if (obj is not IPlayerCharacter member) continue;
                var memberName = member.Name.TextValue;
                if (memberName == player?.Name.TextValue) continue;

                replacements.Add((new string[] { memberName }, GetChangedName(memberName)));
            }

            if (Service.Condition[ConditionFlag.ParticipatingInCrossWorldPartyOrAlliance])
            {
                foreach (var x in InfoProxyCrossRealm.Instance()->CrossRealmGroups[0].GroupMembers)
                {
                    var name = Encoding.UTF8.GetString(x.Name);
                    replacements.Add((new string[] { name }, GetChangedName(name)));
                }
            }
            else
            {
                foreach (var obj in Service.Config.FriendList)
                {
                    replacements.Add((new string[] { obj }, GetChangedName(obj)));
                }
            }

            var friendList = (AddonFriendList*)Service.GameGui.GetAddonByName("FriendList", 1);
            if (friendList == null) return;

            var list = friendList->FriendList;
            for (var i = 0; i < list->ListLength; i++)
            {
                var item = list->ItemRendererList[i];
                var textNode = item.AtkComponentListItemRenderer->AtkComponentButton.ButtonTextNode;

                var text = textNode->NodeText.ToString();
                if (!text.Contains('.') && Service.Config.FriendList.Add(text))
                {
                    Service.Config.SaveConfig();
                }
            }
        }
        finally
        {
            Replacement = replacements;
            IsRunning = false;
        }
    }

    private void AtkTextNodeSetTextDetour(IntPtr node, IntPtr text)
    {
        if (!Service.Config.Enabled)
        {
            AtkTextNodeSetTextHook?.Original(node,text);
            return;
        }
        AtkTextNodeSetTextHook?.Original(node, ChangeName(text));
    }

    private unsafe void SetNamePlateDetour(IntPtr namePlateObjectPtr, bool isPrefixTitle,
        bool displayTitle, IntPtr titlePtr, IntPtr namePtr, IntPtr fcNamePtr, IntPtr prefix, int iconId)
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

                if (Service.Config.FCNameReplace)
                {
                    nameSe = GetSeStringFromPtr(fcNamePtr).TextValue;
                    foreach ((var key, var value) in Service.Config.FCNameDict)
                    {
                        if (key != nameSe) continue;
                        GetPtrFromSeString(value, fcNamePtr);
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Service.Log.Error(ex, "Failed to change name plate");
        }

        //SetNamePlateHook?.Original(namePlateObjectPtr, isPrefixTitle, displayTitle,
        //    titlePtr, namePtr, fcNamePtr, prefix, iconId);
    }

    private static string[] GetNamesFull(string name)
    {
        var names = name.Split(' ');
        if (names.Length != 2) return [name];

        var first = names[0];
        var last = names[1];
        //var firstShort = first.ToUpper()[0] + ".";
        //var lastShort = last.ToUpper()[0] + ".";

        return
        [
            name,
            //$"{first} {lastShort}",
            //$"{firstShort} {last}",
            //$"{firstShort} {lastShort}",
            first, last,
        ];
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
            Service.Log.Error(ex, "Something wrong with change name!");
            return seStringPtr;
        }

        static bool ChangeSeString(SeString seString)
        {
            try
            {
                if (seString.Payloads.All(payload => payload.Type != PayloadType.RawText)) return false;

                return Replacement.Any(pair => ReplacePlayerName(seString, pair.Item1, pair.Item2))
                    || ReplacePlayerName(seString, Service.Config.CharacterNames, Service.Config.FakeNameText);
            }
            catch (Exception ex)
            {
                Service.Log.Error(ex, "Something wrong with replacement!");
                return false;
            }
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

    public static string GetChangedName(string str)
    {
        if (string.IsNullOrEmpty(str)) return str;

        foreach ((var key, var value) in Service.Config.NameDict)
        {
            if (key == str) return value;
        }
        var lt = str.Split(' ');
        if (lt.Length != 2) return str;
        return string.Join(" . ", lt.Select(s => s.ToUpper().FirstOrDefault()));
    }

    private static bool ReplacePlayerName(SeString text, IEnumerable<string> names, string replacement)
    {
        foreach (var name in names)
        {
            if (ReplacePlayerNamePrivate(text, name, replacement))
            {
                return true;
            }
        }
        return false;

        static bool ReplacePlayerNamePrivate(SeString text, string name, string replacement)
        {
            if (string.IsNullOrEmpty(name)) return false;

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
}
