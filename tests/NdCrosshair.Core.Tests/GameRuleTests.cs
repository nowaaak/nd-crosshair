namespace NdCrosshair.Core.Tests;

public class GameRuleTests
{
    [Theory]
    [InlineData("VALORANT-Win64-Shipping.exe", GameMatchKind.Process)]
    [InlineData("  cs2.EXE ", GameMatchKind.Process)]
    [InlineData("Fortnite", GameMatchKind.WindowTitle)]
    [InlineData("Apex Legends", GameMatchKind.WindowTitle)]
    public void FromInput_ClassifiesByExeSuffix(string input, GameMatchKind expected)
    {
        var rule = GameRule.FromInput(input);

        Assert.NotNull(rule);
        Assert.Equal(expected, rule.Kind);
        Assert.Equal(input.Trim(), rule.Pattern);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void FromInput_RejectsEmptyInput(string? input) => Assert.Null(GameRule.FromInput(input));

    [Fact]
    public void ProcessRule_MatchesExactFileNameOnly()
    {
        var rule = new GameRule(GameMatchKind.Process, "cs2.exe");

        Assert.True(rule.Matches(@"C:\Steam\cs2.exe", "Counter-Strike 2"));
        Assert.True(rule.Matches("CS2.EXE", string.Empty));
        Assert.False(rule.Matches("cs2.exe.bak", "cs2"));
        Assert.False(rule.Matches("chrome.exe", "cs2.exe - Google Search"));
        Assert.False(rule.Matches(null, "cs2.exe"));
    }

    [Fact]
    public void TitleRule_MatchesSubstringIgnoringCase()
    {
        var rule = new GameRule(GameMatchKind.WindowTitle, "Fortnite");

        Assert.True(rule.Matches("chrome.exe", "fortnite patch notes"));
        Assert.True(rule.Matches(null, "Fortnite  "));
        Assert.False(rule.Matches("FortniteClient-Win64-Shipping.exe", "Epic Games"));
    }
}
