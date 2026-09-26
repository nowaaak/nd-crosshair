namespace NdCrosshair.Core.Tests;

public class DesignShareCodeTests
{
    private static readonly CrosshairDesign Layered = new()
    {
        Layers =
        [
            new ClassicLayer { Settings = new CrosshairSettings { Gap = 6, Color = new CrosshairColor(10, 200, 30) } },
            new ClassicLayer { Settings = new CrosshairSettings { ShowDot = true, LineLength = 0 }, OffsetY = 12, Scale = 150, Blur = 2 },
        ],
    };

    [Fact]
    public void ClassicOnlyDesign_KeepsCompatibleShareCode()
    {
        var settings = new CrosshairSettings { Gap = 7, ShowDot = true };

        var code = DesignShareCode.Encode(CrosshairDesign.FromClassic(settings));

        Assert.Equal(ShareCode.Encode(settings), code);
        Assert.True(DesignShareCode.TryDecode(code, out var decoded, out _));
        Assert.Equal(CrosshairDesign.FromClassic(settings), decoded);
    }

    [Fact]
    public void LayeredDesign_RoundTripsThroughVersion4Code()
    {
        var code = DesignShareCode.Encode(Layered);

        Assert.StartsWith(DesignShareCode.Prefix, code);
        Assert.All(code[DesignShareCode.Prefix.Length..], character => Assert.True(char.IsAsciiLetterOrDigit(character) || character is '-' or '_'));
        Assert.True(DesignShareCode.TryDecode("  " + code.ToLowerInvariant()[..5] + code[5..] + " ", out var decoded, out var error));
        Assert.Equal(ShareCodeError.None, error);
        Assert.Equal(Layered.Normalize(), decoded);
    }

    [Fact]
    public void TransformedSingleLayer_UsesVersion4Code()
    {
        var design = new CrosshairDesign { Layers = [new ClassicLayer { OffsetX = 4 }] };

        Assert.StartsWith(DesignShareCode.Prefix, DesignShareCode.Encode(design));
    }

    [Fact]
    public void TamperedCode_IsRejected()
    {
        var code = DesignShareCode.Encode(Layered);
        var tampered = code[..^2] + (code[^2] == 'A' ? 'B' : 'A') + code[^1];

        Assert.False(DesignShareCode.TryDecode(tampered, out _, out var error));
        Assert.Contains(error, new[] { ShareCodeError.ChecksumMismatch, ShareCodeError.InvalidLength, ShareCodeError.InvalidEncoding });
    }

    [Theory]
    [InlineData("NDX4-")]
    [InlineData("NDX4-!!!!")]
    [InlineData("NDX4-AA")]
    public void MalformedVersion4Code_IsRejected(string code)
    {
        Assert.False(DesignShareCode.TryDecode(code, out _, out var error));
        Assert.NotEqual(ShareCodeError.None, error);
    }

    [Fact]
    public void OversizedCode_IsRejected()
    {
        Assert.False(DesignShareCode.TryDecode(DesignShareCode.Prefix + new string('A', 9000), out _, out var error));
        Assert.Equal(ShareCodeError.InvalidLength, error);
    }

    [Fact]
    public void UnknownPrefix_IsReportedLikeBefore()
    {
        Assert.False(DesignShareCode.TryDecode("XYZ-123", out _, out var error));
        Assert.Equal(ShareCodeError.UnknownPrefix, error);
    }
}
