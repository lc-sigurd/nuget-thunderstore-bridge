using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using NuGet.Frameworks;
using JsonException = Newtonsoft.Json.JsonException;

namespace Build.Schema.Converters;

public class NuGetFrameworkJsonConverter : JsonConverter<NuGetFramework>
{
    public override NuGetFramework? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is not JsonTokenType.String)
            throw new JsonException($"Found token {reader.TokenType} but expected token {JsonTokenType.String}");

        using var doc = JsonDocument.ParseValue(ref reader);
        return NuGetFramework.Parse(doc.RootElement.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, NuGetFramework value, JsonSerializerOptions options)
    {
        var s = value.GetShortFolderName();
        writer.WriteStringValue(s);
    }
}
