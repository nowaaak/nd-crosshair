using System.Text.Json;
using System.Text.Json.Serialization;

namespace NdCrosshair.Core;

public enum ConfigLoadStatus
{
    Loaded,
    Created,
    RecoveredFromCorruptFile,
}

public sealed record ConfigLoadResult(AppConfig Config, ConfigLoadStatus Status, string? CorruptFileBackupPath);

public sealed class ConfigStore
{
    private const long MaxImportFileSize = 1024 * 1024;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new CrosshairColorJsonConverter(), new JsonStringEnumConverter() },
    };

    public ConfigStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        FilePath = filePath;
    }

    public static string DefaultFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "NdCrosshair",
        "config.json");

    public string FilePath { get; }

    public static void Export(AppConfig config, string path)
    {
        ArgumentNullException.ThrowIfNull(config);
        WriteAtomically(path, Serialize(config));
    }

    public static AppConfig Import(string path)
    {
        var info = new FileInfo(path);
        if (!info.Exists)
        {
            throw new FileNotFoundException("Configuration file not found.", path);
        }

        if (info.Length > MaxImportFileSize)
        {
            throw new InvalidDataException("Configuration file is too large.");
        }

        try
        {
            return Deserialize(File.ReadAllText(path));
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Configuration file is not valid.", exception);
        }
    }

    public ConfigLoadResult Load()
    {
        if (!File.Exists(FilePath))
        {
            return new ConfigLoadResult(new AppConfig().Normalize(), ConfigLoadStatus.Created, null);
        }

        var json = File.ReadAllText(FilePath);
        try
        {
            return new ConfigLoadResult(Deserialize(json), ConfigLoadStatus.Loaded, null);
        }
        catch (JsonException)
        {
            var backupPath = $"{FilePath}.corrupt-{DateTime.Now:yyyyMMdd-HHmmss}";
            File.Move(FilePath, backupPath, overwrite: true);
            return new ConfigLoadResult(new AppConfig().Normalize(), ConfigLoadStatus.RecoveredFromCorruptFile, backupPath);
        }
    }

    public void Save(AppConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        WriteAtomically(FilePath, Serialize(config));
    }

    private static string Serialize(AppConfig config) => JsonSerializer.Serialize(config.Normalize(), SerializerOptions);

    private static AppConfig Deserialize(string json) =>
        (JsonSerializer.Deserialize<AppConfig>(json, SerializerOptions)
            ?? throw new JsonException("Configuration file is empty.")).Normalize();

    private static void WriteAtomically(string path, string content)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = path + ".tmp";
        File.WriteAllText(tempPath, content);
        File.Move(tempPath, path, overwrite: true);
    }

    private sealed class CrosshairColorJsonConverter : JsonConverter<CrosshairColor>
    {
        public override CrosshairColor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var text = reader.GetString();
            return CrosshairColor.TryParseHex(text, out var color)
                ? color
                : throw new JsonException($"Invalid color value '{text}'.");
        }

        public override void Write(Utf8JsonWriter writer, CrosshairColor value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToHex());
    }
}
