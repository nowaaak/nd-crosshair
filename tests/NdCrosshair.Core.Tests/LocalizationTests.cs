using System.Text.RegularExpressions;
using NdCrosshair.Core.Localization;

namespace NdCrosshair.Core.Tests;

public partial class LocalizationTests
{
    [Fact]
    public void Languages_HaveIdenticalKeys()
    {
        Assert.Equal(UiStrings.German.Keys.Order(), UiStrings.English.Keys.Order());
    }

    [Fact]
    public void Languages_HaveNoEmptyValuesAndMatchingPlaceholders()
    {
        foreach (var (key, german) in UiStrings.German)
        {
            var english = UiStrings.English[key];
            Assert.False(string.IsNullOrWhiteSpace(german), key);
            Assert.False(string.IsNullOrWhiteSpace(english), key);
            Assert.Equal(Placeholders(german), Placeholders(english));
        }
    }

    [Fact]
    public void EveryKeyUsedInTheApp_Exists()
    {
        var appDirectory = FindAppDirectory();
        var used = new SortedSet<string>();

        foreach (var file in Directory.EnumerateFiles(appDirectory, "*.xaml", SearchOption.AllDirectories))
        {
            foreach (Match match in XamlKey().Matches(File.ReadAllText(file)))
            {
                used.Add(match.Groups[1].Value);
            }
        }

        foreach (var file in Directory.EnumerateFiles(appDirectory, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            foreach (Match match in CodeKey().Matches(text))
            {
                used.Add(match.Groups[1].Value);
            }

            foreach (Match match in TernaryKey().Matches(text))
            {
                used.Add(match.Groups[1].Value);
                used.Add(match.Groups[2].Value);
            }
        }

        Assert.NotEmpty(used);
        var missing = used.Where(key => !UiStrings.German.ContainsKey(key)).ToList();
        Assert.True(missing.Count == 0, "Missing keys: " + string.Join(", ", missing));
    }

    private static string[] Placeholders(string text) =>
        PlaceholderPattern().Matches(text).Select(match => match.Value).Distinct().Order().ToArray();

    private static string FindAppDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "NdCrosshair.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine(directory.FullName, "src", "NdCrosshair.App");
    }

    [GeneratedRegex(@"\{l:T (\w+)\}")]
    private static partial Regex XamlKey();

    [GeneratedRegex(@"Loc\.(?:T|Format)\(\s*""(\w+)""")]
    private static partial Regex CodeKey();

    [GeneratedRegex(@"Loc\.T\(\w+ \? ""(\w+)"" : ""(\w+)""\)")]
    private static partial Regex TernaryKey();

    [GeneratedRegex(@"\{\d+\}")]
    private static partial Regex PlaceholderPattern();
}
