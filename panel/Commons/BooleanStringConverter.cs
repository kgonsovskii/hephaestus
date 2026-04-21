using System.Text.Json;
using System.Text.Json.Serialization;

namespace Commons;

/// <summary>JSON converter for boolean values serialized as the strings <c>true</c>/<c>false</c>.</summary>
public sealed class BooleanStringConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (string.Equals(stringValue, "true", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(stringValue, "false", StringComparison.OrdinalIgnoreCase))
                return false;
        }

        throw new JsonException("Invalid boolean string value");
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value ? "true" : "false");
    }
}
