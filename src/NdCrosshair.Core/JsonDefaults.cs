using System.Text.Json;
using System.Text.Json.Serialization;

namespace NdCrosshair.Core;

internal static class JsonDefaults
{
    public static JsonSerializerOptions Indented { get; } = Create(writeIndented: true);

    public static JsonSerializerOptions Compact { get; } = Create(writeIndented: false);

    private static JsonSerializerOptions Create(bool writeIndented) => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = writeIndented,
        Converters = { new CrosshairColorJsonConverter(), new ColorModeJsonConverter(), new JsonStringEnumConverter() },
    };

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
