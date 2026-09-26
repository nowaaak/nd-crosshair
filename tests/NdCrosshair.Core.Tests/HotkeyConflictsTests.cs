namespace NdCrosshair.Core.Tests;

public class HotkeyConflictsTests
{
    [Fact]
    public void FindDuplicates_FlagsLaterUsesOfTheSameBindingOnly()
    {
        var f8 = new HotkeyBinding(0, 0x77);
        var ctrlOne = new HotkeyBinding(HotkeyBinding.ModifierControl, 0x31);

        var duplicates = HotkeyConflicts.FindDuplicates([f8, ctrlOne, HotkeyBinding.None, f8, HotkeyBinding.None, ctrlOne with { }]);

        Assert.Equal(new HashSet<int> { 3, 5 }, duplicates);
    }

    [Fact]
    public void FindDuplicates_TreatsDifferentModifiersAsDistinct()
    {
        var duplicates = HotkeyConflicts.FindDuplicates(
        [
            new HotkeyBinding(0, 0x31),
            new HotkeyBinding(HotkeyBinding.ModifierShift, 0x31),
            new HotkeyBinding(HotkeyBinding.ModifierShift | 0x100, 0x31),
        ]);

        Assert.Equal(new HashSet<int> { 2 }, duplicates);
    }
}
