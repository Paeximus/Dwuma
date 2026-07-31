using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dwuma.Converters;

public sealed class FlexibleStringListConverter
    : JsonConverter<List<string>>
{
    public override List<string> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var values = new List<string>();

        if (reader.TokenType == JsonTokenType.Null)
        {
            return values;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            string? value = reader.GetString();

            if (!string.IsNullOrWhiteSpace(value))
            {
                values.Add(value.Trim());
            }

            return values;
        }

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    return values;
                }

                if (reader.TokenType == JsonTokenType.String)
                {
                    string? value = reader.GetString();

                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        values.Add(value.Trim());
                    }
                }
                else if (reader.TokenType == JsonTokenType.Null)
                {
                    continue;
                }
                else
                {
                    using JsonDocument document =
                        JsonDocument.ParseValue(ref reader);

                    string value =
                        document.RootElement.ToString();

                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        values.Add(value);
                    }
                }
            }
        }

        using JsonDocument fallbackDocument =
            JsonDocument.ParseValue(ref reader);

        string fallback =
            fallbackDocument.RootElement.ToString();

        if (!string.IsNullOrWhiteSpace(fallback))
        {
            values.Add(fallback);
        }

        return values;
    }

    public override void Write(
        Utf8JsonWriter writer,
        List<string> value,
        JsonSerializerOptions options)
    {
        writer.WriteStartArray();

        foreach (string item in value)
        {
            writer.WriteStringValue(item);
        }

        writer.WriteEndArray();
    }
}