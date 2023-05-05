using Dalamud.Game.Libc;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace FakeName;

internal static class Replacer 
{
    public static IntPtr ChangeName(IntPtr seStringPtr)
    {
        var str = SeStringFromPtr(seStringPtr);
        if(ChangeSeString(ref str))
        {
            FreePtr(seStringPtr);
            return SeStringToPtr(str);
        }
        else
        {
            return seStringPtr;
        }
    }
    
    static SeString SeStringFromPtr(IntPtr seStringPtr)
    {
        return SeString.Parse(StdString.ReadFromPointer(seStringPtr).RawData);
    }

    public static IntPtr SeStringToPtr(SeString seString)
    {
        var bytes = seString.Encode();
        var pointer = Marshal.AllocHGlobal(bytes.Length + 1);
        Marshal.Copy(bytes, 0, pointer, bytes.Length);
        Marshal.WriteByte(pointer, bytes.Length, 0);

        return pointer;
    }

    public static void FreePtr(IntPtr seStringPtr)
    {
        Marshal.FreeHGlobal(seStringPtr);
    }

    public static bool ChangeSeString(ref SeString seString)
    {
        if (!Service.Config.Enabled) return false;

        if (seString.Payloads.All(payload => payload.Type != PayloadType.RawText)) return false;

        var player = Service.ClientState.LocalPlayer;
        if (player == null) return false;

        var result = ReplacePlayerName(seString, GetNames(player.Name.TextValue), Service.Config.FakeNameText);

        if (Service.Config.PartyMemberReplace)
        {
            foreach (var member in Service.PartyList)
            {
                var memberName = member.Name.TextValue;
                if (memberName == player.Name.TextValue) continue;

                var jobData = member.ClassJob.GameData;
                if (jobData == null) continue;

                var nickName = string.Join(' ', memberName.Split(' ').Select(s => s.ToUpper()[0] + "."));
                var memberReplace = $"{jobData.Name.RawString}[{nickName}]";

                result = ReplacePlayerName(seString, GetNames(memberName), memberReplace) || result;
            }
        }

        return result;
    }

    private static string[] GetNames(string name)
    {
        var names = name.Split(' ');
        if (names.Length != 2) return new string[] { name };

        var first = names[0];
        var last = names[1];
        var firstShort = first.ToUpper()[0] + ".";
        var lastShort = last.ToUpper()[0] + ".";

        return new string[]
        {
            name, first, last,
            $"{first} {lastShort}",
            $"{firstShort} {last}",
            $"{firstShort} {lastShort}",
        };
    }

    private static bool ReplacePlayerName(this SeString text, string[] names, string replacement)
    {
        var result = false;
        foreach (var name in names)
        {
            if(ReplacePlayerName(text, name, replacement))
            {
                result = true;
            }
        }
        return result;
    }

    private static bool ReplacePlayerName(this SeString text, string name, string replacement)
    {
        if (string.IsNullOrEmpty(name)) return false;

        var result = false;
        foreach (var payLoad in text.Payloads)
        {
            if (payLoad is TextPayload load)
            {
                var t = load.Text.Replace(name, replacement);
                if (t == load.Text) continue;
                load.Text = t;
                return true;
            }
        }
        return result;
    }
}