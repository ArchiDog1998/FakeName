using System.ComponentModel;

namespace FakeName;

internal enum UiString : byte
{
    [Description("The FC replacement only effect on the nameplate.")]
    FC,

    [Description("Original Name")]
    Origin,

    [Description("Replaced Name")]
    Replace,

    [Description("Delete")]
    Delete,
}
