using System.Text.Json.Serialization;

namespace Build.Schema.Thunderstore.API;

public class ThunderstorePackageListing
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("owner")]
    public required string Namespace { get; init; }

    [JsonPropertyName("versions")]
    public required ThunderstorePackageVersionListing[] Versions { get; init; }
}
