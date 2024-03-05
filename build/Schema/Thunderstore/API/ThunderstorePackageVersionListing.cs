using System;
using System.Text.Json.Serialization;

namespace Build.Schema.Thunderstore.API;

public class ThunderstorePackageVersionListing
{
    [JsonPropertyName("version_number")]
    public required Version Version { get; init; }
}
