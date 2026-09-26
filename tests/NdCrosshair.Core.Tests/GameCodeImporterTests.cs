namespace NdCrosshair.Core.Tests;

public class GameCodeImporterTests
{
    private const string Cs2StaticCross = "CSGO-hLbCn-69VT6-Bok83-9MOqW-SWzwQ";
    private const string Cs2DotOnly = "CSGO-MnUCC-89iG7-2cVar-wy7Yn-amCpF";
    private const string Cs2StaticCircle = "CSGO-p4NqQ-mV2es-p2UF3-Q9HWe-fVmoE";
    private const string Cs2StaticSquare = "CSGO-G8oAC-RyvWc-Hi3CZ-voSwn-QJbfE";
    private const string Cs2DynamicLegacy = "CSGO-sP6xU-TSyN9-sZcO5-2D48M-UppkP";
    private const string Cs2LegacyFormat = "CSGO-Cn37R-YE7vo-pLCAL-aURmZ-z6zkG";

    [Fact]
    public void Cs2_DecodesPixelFormatVersion3()
    {
        Assert.True(Cs2CrosshairCode.TryDecode(Cs2DotOnly, out var crosshair, out var error));

        Assert.Equal(GameCodeError.None, error);
        Assert.Equal(new Cs2Crosshair(3, 6, false, true, false, new CrosshairColor(252, 15, 192), 255, 4, 8, 3, 1, 1080), crosshair);
    }

    [Fact]
    public void Cs2_DecodesPixelFormatVersion4()
    {
        Assert.True(Cs2CrosshairCode.TryDecode(Cs2StaticSquare, out var square, out _));
        Assert.True(Cs2CrosshairCode.TryDecode(Cs2DynamicLegacy, out var legacy, out _));

        Assert.Equal(new Cs2Crosshair(4, 8, false, true, false, new CrosshairColor(124, 57, 57), 255, 25, 7, 20, 1, 768), square);
        Assert.Equal(new Cs2Crosshair(4, 2, true, true, false, new CrosshairColor(255, 0, 0), 255, 0, 5, 1, 0, 768), legacy);
    }

    [Fact]
    public void Cs2_StaticCrossAtSameResolution_MapsOneToOne()
    {
        var import = Import(Cs2StaticCross, 1080);

        Assert.Equal(GameCodeSource.CounterStrike2, import.Source);
        Assert.Empty(import.Notes);
        var s = import.Settings;
        Assert.True(s.ShowTop && s.ShowBottom && s.ShowLeft && s.ShowRight);
        Assert.Equal((4, 8, 2), (s.Gap, s.LineLength, s.LineThickness));
        Assert.Equal(new CrosshairColor(50, 250, 50), s.Color);
        Assert.Equal(100, s.Opacity);
        Assert.False(s.ShowOutline);
        Assert.False(s.ShowDot);
        Assert.False(s.ShowRing);
    }

    [Fact]
    public void Cs2_ScalesPixelsToTargetScreenHeight()
    {
        var s = Import(Cs2StaticCross, 1440).Settings;

        Assert.Equal((5, 11, 3), (s.Gap, s.LineLength, s.LineThickness));
    }

    [Fact]
    public void Cs2_DotOnly_HidesLinesAndShowsDot()
    {
        var import = Import(Cs2DotOnly, 1080);

        Assert.False(import.Settings.ShowTop || import.Settings.ShowBottom || import.Settings.ShowLeft || import.Settings.ShowRight);
        Assert.True(import.Settings.ShowDot);
        Assert.Equal(3, import.Settings.DotSize);
        Assert.True(import.Settings.ShowOutline);
    }

    [Fact]
    public void Cs2_ApproximatedStyles_AreReported()
    {
        Assert.Contains(GameCodeNote.CircleApproximated, Import(Cs2StaticCircle, 1080).Notes);
        Assert.True(Import(Cs2StaticCircle, 1080).Settings.ShowRing);
        Assert.Contains(GameCodeNote.SquareApproximated, Import(Cs2StaticSquare, 768).Notes);

        var legacy = Import(Cs2DynamicLegacy, 768).Notes;
        Assert.Contains(GameCodeNote.DynamicShownStatic, legacy);
        Assert.Contains(GameCodeNote.FollowRecoilIgnored, legacy);
    }

    [Fact]
    public void Cs2_LegacyUnitFormat_IsRejectedAsOutdated()
    {
        Assert.False(GameCodeImporter.TryImport(Cs2LegacyFormat, 1080, out var result, out var error));

        Assert.Null(result);
        Assert.Equal(GameCodeError.OutdatedCs2Format, error);
    }

    [Theory]
    [InlineData("CSGO-hLbCn-69VT6-Bok83-9MOqW-SWzwl")]
    [InlineData("CSGO-hLbCn-69VT6-Bok83-9MOqW")]
    [InlineData("CSGO-hLbCn-69VT6-Bok83-9MOqW-SWzwQ-AAAAA")]
    public void Cs2_MalformedCode_IsInvalid(string code)
    {
        Assert.False(GameCodeImporter.TryImport(code, 1080, out _, out var error));
        Assert.Equal(GameCodeError.Invalid, error);
    }

    [Fact]
    public void Cs2_AlteredCode_FailsChecksum()
    {
        Assert.False(GameCodeImporter.TryImport("CSGO-hLbCn-69VT6-Bok83-9MOqW-SWzwR", 1080, out _, out var error));
        Assert.Contains(error, new[] { GameCodeError.ChecksumMismatch, GameCodeError.Invalid });
    }

    [Fact]
    public void Valorant_ExplicitValues_MapExactly()
    {
        var import = Import("0;P;c;5;h;0;0l;4;0o;2;0a;1;0f;0;1b;0", 1080);

        Assert.Equal(GameCodeSource.Valorant, import.Source);
        Assert.Empty(import.Notes);
        var s = import.Settings;
        Assert.Equal(new CrosshairColor(0, 255, 255), s.Color);
        Assert.Equal((2, 4, 2), (s.Gap, s.LineLength, s.LineThickness));
        Assert.Equal(100, s.Opacity);
        Assert.False(s.ShowOutline);
        Assert.False(s.ShowDot);
    }

    [Fact]
    public void Valorant_DefaultCode_UsesGameDefaultsAndReportsApproximations()
    {
        var import = Import("0", 1080);

        var s = import.Settings;
        Assert.Equal(new CrosshairColor(255, 255, 255), s.Color);
        Assert.Equal((3, 6, 2), (s.Gap, s.LineLength, s.LineThickness));
        Assert.Equal(80, s.Opacity);
        Assert.True(s.ShowOutline);
        Assert.Equal(1, s.OutlineThickness);
        Assert.Contains(GameCodeNote.OuterLinesIgnored, import.Notes);
        Assert.Contains(GameCodeNote.DynamicShownStatic, import.Notes);
        Assert.Contains(GameCodeNote.OutlineOpacityApproximated, import.Notes);
    }

    [Fact]
    public void Valorant_DotOnly_HidesLines()
    {
        var s = Import("0;P;c;1;h;0;d;1;z;3;a;1;0b;0;1b;0", 1080).Settings;

        Assert.False(s.ShowTop || s.ShowBottom || s.ShowLeft || s.ShowRight);
        Assert.True(s.ShowDot);
        Assert.Equal(3, s.DotSize);
        Assert.Equal(new CrosshairColor(0, 255, 0), s.Color);
        Assert.Equal(100, s.Opacity);
    }

    [Fact]
    public void Valorant_CustomColor_UsesHexValue()
    {
        var s = Import("0;P;c;8;u;FF8800FF;h;0;0f;0;1b;0", 1080).Settings;

        Assert.Equal(new CrosshairColor(0xFF, 0x88, 0x00), s.Color);
    }

    [Fact]
    public void Valorant_OnlyPrimarySectionIsUsed()
    {
        var s = Import("0;s;1;c;1;P;c;7;h;0;0f;0;1b;0;A;c;1;S;c;0;s;0.5", 1080).Settings;

        Assert.Equal(new CrosshairColor(255, 0, 0), s.Color);
    }

    [Fact]
    public void Valorant_ReportsSeparateVerticalLengthAndClampedValues()
    {
        Assert.Contains(GameCodeNote.SeparateVerticalLengthIgnored, Import("0;P;h;0;0g;1;0v;10;0f;0;1b;0", 1080).Notes);

        var clamped = Import("0;P;t;6;o;1;0f;0;1b;0", 1080);
        Assert.Contains(GameCodeNote.ValuesClamped, clamped.Notes);
        Assert.Equal(CrosshairSettings.MaxOutlineThickness, clamped.Settings.OutlineThickness);
    }

    [Theory]
    [InlineData("0;P;c;9")]
    [InlineData("0;P;c")]
    [InlineData("0;P;0l;abc")]
    [InlineData("0;P;c;8")]
    [InlineData("0;P;c;8;u;XYZ")]
    [InlineData("0;P;c;1.5")]
    public void Valorant_MalformedCode_IsInvalid(string code)
    {
        Assert.False(GameCodeImporter.TryImport(code, 1080, out _, out var error));
        Assert.Equal(GameCodeError.Invalid, error);
    }

    [Theory]
    [InlineData("", GameCodeError.Empty)]
    [InlineData("   ", GameCodeError.Empty)]
    [InlineData("hello", GameCodeError.Unrecognized)]
    [InlineData("NDX3-AAAA", GameCodeError.Unrecognized)]
    public void UnknownInput_IsRejected(string code, GameCodeError expected)
    {
        Assert.False(GameCodeImporter.TryImport(code, 1080, out _, out var error));
        Assert.Equal(expected, error);
    }

    [Fact]
    public void IsGameCode_DetectsSupportedFormatsOnly()
    {
        Assert.True(GameCodeImporter.IsGameCode(Cs2StaticCross));
        Assert.True(GameCodeImporter.IsGameCode(" 0;P;c;1 "));
        Assert.False(GameCodeImporter.IsGameCode(ShareCode.Encode(new CrosshairSettings())));
        Assert.False(GameCodeImporter.IsGameCode(null));
    }

    private static GameCodeImport Import(string code, int screenHeight)
    {
        Assert.True(GameCodeImporter.TryImport(code, screenHeight, out var result, out var error), error.ToString());
        Assert.Equal(GameCodeError.None, error);
        return Assert.IsType<GameCodeImport>(result);
    }
}
