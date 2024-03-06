using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Build.Schema.Converters;

namespace Build.Schema.Thunderstore.API;

public class ThunderstorePackageListing
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("owner")]
    public required string Namespace { get; init; }

    [JsonConverter(typeof(ThunderstorePackageVersionListingIndexJsonConverter))]
    [JsonPropertyName("versions")]
    public required Dictionary<Version, ThunderstorePackageVersionListing>  Versions { get; init; }

    [JsonIgnore]
    private ThunderstorePackageVersionListing? _latestVersion;

    [JsonIgnore]
    public ThunderstorePackageVersionListing LatestVersion => _latestVersion ??= ComputeLatestVersion();

    private ThunderstorePackageVersionListing ComputeLatestVersion() => Versions.Values
        .MaxBy(versionListing => versionListing.Version)!;
}
