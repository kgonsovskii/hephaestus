using System.Text.Json;
using System.Text.Json.Serialization;

namespace Refiner;

public class BooleanStringConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string stringValue = reader.GetString();
            if (string.Equals(stringValue, "true", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            else if (string.Equals(stringValue, "false", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        throw new JsonException("Invalid boolean string value");
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value ? "true" : "false");
    }
}