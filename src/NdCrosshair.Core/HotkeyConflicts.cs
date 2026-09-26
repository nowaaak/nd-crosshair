namespace NdCrosshair.Core;

public static class HotkeyConflicts
{
    public static IReadOnlySet<int> FindDuplicates(IReadOnlyList<HotkeyBinding> bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);

        var seen = new HashSet<HotkeyBinding>();
        var duplicates = new HashSet<int>();
        for (var i = 0; i < bindings.Count; i++)
        {
            var binding = bindings[i].Normalize();
            if (!binding.IsNone && !seen.Add(binding))
            {
                duplicates.Add(i);
            }
        }

        return duplicates;
    }
}
