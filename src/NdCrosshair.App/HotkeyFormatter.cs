using System.Windows.Input;
using NdCrosshair.App.Localization;
using NdCrosshair.Core;

namespace NdCrosshair.App;

internal static class HotkeyFormatter
{
    public static string Format(HotkeyBinding binding)
    {
        if (binding.IsNone)
        {
            return Loc.T("HotkeyNone");
        }

        var parts = new List<string>();
        if ((binding.Modifiers & HotkeyBinding.ModifierControl) != 0)
        {
            parts.Add(Loc.T("KeyControl"));
        }

        if ((binding.Modifiers & HotkeyBinding.ModifierAlt) != 0)
        {
            parts.Add("Alt");
        }

        if ((binding.Modifiers & HotkeyBinding.ModifierShift) != 0)
        {
            parts.Add(Loc.T("KeyShift"));
        }

        if ((binding.Modifiers & HotkeyBinding.ModifierWin) != 0)
        {
            parts.Add("Win");
        }

        parts.Add(KeyInterop.KeyFromVirtualKey(binding.VirtualKey).ToString());
        return string.Join(" + ", parts);
    }
}
