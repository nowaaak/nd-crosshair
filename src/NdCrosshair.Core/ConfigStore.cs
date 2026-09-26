using System.Text.Json;
using System.Text.Json.Serialization;

namespace NdCrosshair.Core;

public enum ConfigLoadStatus
{
    Loaded,
    Created,
    RecoveredFromCorruptFile,
    Unreadable,
}

public sealed record ConfigLoadResult(AppConfig Config, ConfigLoadStatus Status, string? CorruptFileBackupPath, string? ErrorMessage = null);

public sealed class ConfigStore
{
    private const long MaxImportFileSize = 1024 * 1024;
    private const int ReadAttempts = 3;
    private static readonly TimeSpan ReadRetryDelay = TimeSpan.FromMilliseconds(100);

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new CrosshairColorJsonConverter(), new ColorModeJsonConverter(), new JsonStringEnumConverter() },
    };

    public ConfigStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        FilePath = filePath;
    }

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

        if (!TryRead(FilePath, out var json, out var readError))
        {
            return new ConfigLoadResult(new AppConfig().Normalize(), ConfigLoadStatus.Unreadable, null, readError);
        }

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

    private static bool TryRead(string path, out string content, out string? error)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                content = File.ReadAllText(path);
                error = null;
                return true;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                if (attempt >= ReadAttempts)
                {
                    content = string.Empty;
                    error = exception.Message;
                    return false;
                }

                Thread.Sleep(ReadRetryDelay);
            }
        }
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

    private sealed class ColorModeJsonConverter : JsonConverter<CrosshairColorMode>
    {
        public override CrosshairColorMode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType switch
            {
                JsonTokenType.String when Enum.TryParse<CrosshairColorMode>(reader.GetString(), ignoreCase: true, out var mode)
                    && Enum.IsDefined(mode) => mode,
                JsonTokenType.Number when reader.TryGetInt32(out var number)
                    && Enum.IsDefined((CrosshairColorMode)number) => (CrosshairColorMode)number,
                JsonTokenType.String or JsonTokenType.Number => CrosshairColorMode.Static,
                _ => throw new JsonException("Invalid color mode."),
            };

        public override void Write(Utf8JsonWriter writer, CrosshairColorMode value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToString());
    }
}
