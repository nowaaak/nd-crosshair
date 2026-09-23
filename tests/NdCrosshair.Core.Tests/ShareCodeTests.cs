using NdCrosshair.Core;

namespace NdCrosshair.Core.Tests;

public class ShareCodeTests
{
    private static readonly CrosshairSettings Custom = new()
    {
        ShowTop = false,
        ShowBottom = true,
        ShowLeft = true,
        ShowRight = false,
        LineLength = 17,
        LineThickness = 3,
        Gap = 0,
        ShowDot = true,
        DotSize = 5,
        ShowOutline = false,
        OutlineThickness = 4,
        Color = new CrosshairColor(12, 34, 56),
        OutlineColor = new CrosshairColor(250, 128, 1),
        Opacity = 73,
        Rotation = 45,
        RoundDot = true,
        ShowRing = true,
        RingRadius = 33,
        RingThickness = 7,
        ShowShadow = true,
        ShadowSize = 9,
        ShadowOpacity = 42,
        ShadowColor = new CrosshairColor(9, 8, 7),
        ColorMode = CrosshairColorMode.Adaptive,
        RainbowSpeed = 7,
    };

    [Fact]
    public void EncodeAndDecode_RoundTripsAllFields()
    {
        var code = ShareCode.Encode(Custom);

        Assert.StartsWith(ShareCode.Prefix, code);
        Assert.True(ShareCode.TryDecode(code, out var decoded, out var error));
        Assert.Equal(ShareCodeError.None, error);
        Assert.Equal(Custom, decoded);
    }

    [Fact]
    public void Encode_ProducesUrlSafeCharactersOnly()
    {
        var code = ShareCode.Encode(new CrosshairSettings { Color = new CrosshairColor(255, 255, 255) });
        var body = code[ShareCode.Prefix.Length..];

        Assert.All(body, character => Assert.True(char.IsAsciiLetterOrDigit(character) || character is '-' or '_'));
    }

    [Fact]
    public void TryDecode_AcceptsWhitespaceAndLowercasePrefix()
    {
        var code = ShareCode.Encode(Custom);
        var variant = "  " + ShareCode.Prefix.ToLowerInvariant() + code[ShareCode.Prefix.Length..] + "\n";

        Assert.True(ShareCode.TryDecode(variant, out var decoded, out _));
        Assert.Equal(Custom, decoded);
    }

    [Theory]
    [InlineData(null, ShareCodeError.Empty)]
    [InlineData("", ShareCodeError.Empty)]
    [InlineData("   ", ShareCodeError.Empty)]
    [InlineData("ABC-123", ShareCodeError.UnknownPrefix)]
    [InlineData("NDX1-", ShareCodeError.InvalidLength)]
    [InlineData("NDX1-!!!!", ShareCodeError.InvalidEncoding)]
    [InlineData("NDX1-AAAA", ShareCodeError.InvalidLength)]
    public void TryDecode_RejectsInvalidInput(string? code, ShareCodeError expected)
    {
        Assert.False(ShareCode.TryDecode(code, out _, out var error));
        Assert.Equal(expected, error);
    }

    [Fact]
    public void TryDecode_RejectsOversizedInput()
    {
        Assert.False(ShareCode.TryDecode(ShareCode.Prefix + new string('A', 10_000), out _, out var error));
        Assert.Equal(ShareCodeError.InvalidLength, error);
    }

    [Fact]
    public void TryDecode_RejectsTruncatedCode()
    {
        var code = ShareCode.Encode(Custom);

        Assert.False(ShareCode.TryDecode(code[..^4], out _, out _));
    }

    [Fact]
    public void TryDecode_DetectsEveryAlteredCharacter()
    {
        var code = ShareCode.Encode(Custom);

        for (var index = ShareCode.Prefix.Length; index < code.Length; index++)
        {
            var replacement = code[index] == 'A' ? 'B' : 'A';
            var tampered = code[..index] + replacement + code[(index + 1)..];

            Assert.False(ShareCode.TryDecode(tampered, out _, out _), $"Tampered code at index {index} was accepted.");
        }
    }

    [Fact]
    public void TryDecode_ReadsLegacyCodes()
    {
        byte[] payload = [0b0001_0101, 8, 3, 2, 4, 2, 80, 1, 2, 3, 4, 5, 6];

        Assert.True(ShareCode.TryDecode(BuildCode(ShareCode.LegacyPrefix, payload), out var settings, out _));
        Assert.Equal(
            new CrosshairSettings
            {
                ShowTop = true,
                ShowBottom = false,
                ShowLeft = true,
                ShowRight = false,
                ShowDot = true,
                ShowOutline = false,
                LineLength = 8,
                LineThickness = 3,
                Gap = 2,
                DotSize = 4,
                OutlineThickness = 2,
                Opacity = 80,
                Color = new CrosshairColor(1, 2, 3),
                OutlineColor = new CrosshairColor(4, 5, 6),
            },
            settings);
    }

    [Fact]
    public void TryDecode_RejectsCurrentPayloadWithLegacyPrefix()
    {
        var body = ShareCode.Encode(Custom)[ShareCode.Prefix.Length..];

        Assert.False(ShareCode.TryDecode(ShareCode.LegacyPrefix + body, out _, out var error));
        Assert.Equal(ShareCodeError.InvalidLength, error);
    }

    [Fact]
    public void TryDecode_ClampsOutOfRangeValues()
    {
        byte[] payload = [0b0011_1111, 250, 0, 255, 0, 99, 1, 1, 2, 3, 4, 5, 6];
        var code = BuildCode(ShareCode.LegacyPrefix, payload);

        Assert.True(ShareCode.TryDecode(code, out var settings, out _));
        Assert.Equal(CrosshairSettings.MaxLineLength, settings.LineLength);
        Assert.Equal(CrosshairSettings.MinLineThickness, settings.LineThickness);
        Assert.Equal(CrosshairSettings.MaxGap, settings.Gap);
        Assert.Equal(CrosshairSettings.MinDotSize, settings.DotSize);
        Assert.Equal(CrosshairSettings.MaxOutlineThickness, settings.OutlineThickness);
        Assert.Equal(CrosshairSettings.MinOpacity, settings.Opacity);
    }

    [Fact]
    public void TryDecode_RejectsUnknownFlags()
    {
        byte[] payload = [0b1000_0000, 6, 2, 4, 2, 1, 100, 0, 255, 0, 0, 0, 0];

        Assert.False(ShareCode.TryDecode(BuildCode(ShareCode.LegacyPrefix, payload), out _, out var error));
        Assert.Equal(ShareCodeError.UnknownFlags, error);
    }

    [Fact]
    public void TryDecode_RejectsUnknownExtraFlags()
    {
        var payload = new byte[22];
        payload[1] = 0b0000_0010;

        Assert.False(ShareCode.TryDecode(BuildCode(ShareCode.Version2Prefix, payload), out _, out var error));
        Assert.Equal(ShareCodeError.UnknownFlags, error);
    }

    [Fact]
    public void TryDecode_ClampsOutOfRangeValuesInCurrentFormat()
    {
        var payload = new byte[22];
        payload[8] = 200;
        payload[9] = 255;
        payload[10] = 0;
        payload[11] = 99;
        payload[12] = 0;

        Assert.True(ShareCode.TryDecode(BuildCode(ShareCode.Version2Prefix, payload), out var settings, out _));
        Assert.Equal(CrosshairSettings.MaxRotation, settings.Rotation);
        Assert.Equal(CrosshairSettings.MaxRingRadius, settings.RingRadius);
        Assert.Equal(CrosshairSettings.MinRingThickness, settings.RingThickness);
        Assert.Equal(CrosshairSettings.MaxShadowSize, settings.ShadowSize);
        Assert.Equal(CrosshairSettings.MinShadowOpacity, settings.ShadowOpacity);
    }

    [Fact]
    public void TryDecode_Version2Code_UsesStaticColorMode()
    {
        var payload = new byte[22];
        payload[3] = 2;

        Assert.True(ShareCode.TryDecode(BuildCode(ShareCode.Version2Prefix, payload), out var settings, out _));
        Assert.Equal(CrosshairColorMode.Static, settings.ColorMode);
        Assert.Equal(new CrosshairSettings().RainbowSpeed, settings.RainbowSpeed);
    }

    [Fact]
    public void TryDecode_RejectsUnknownColorMode()
    {
        var payload = new byte[24];
        payload[22] = 9;

        Assert.False(ShareCode.TryDecode(BuildCode(ShareCode.Prefix, payload), out _, out var error));
        Assert.Equal(ShareCodeError.UnknownFlags, error);
    }

    [Fact]
    public void TryDecode_Version2PayloadWithCurrentPrefix_IsRejected()
    {
        Assert.False(ShareCode.TryDecode(BuildCode(ShareCode.Prefix, new byte[22]), out _, out var error));
        Assert.Equal(ShareCodeError.InvalidLength, error);
    }

    private static string BuildCode(string prefix, byte[] payload)
    {
        var data = payload.Append(ShareCode.Crc8(payload)).ToArray();
        return prefix + Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
