using System;
using System.Text.Json.Serialization;
using NuGet.Versioning;

namespace Build.Schema.Thunderstore.API;

public class ThunderstorePackageVersionListing
{
    [JsonPropertyName("version_number")]
    public required Version Version { get; init; }

    [JsonPropertyName("date_created")]
    public required DateTimeOffset DateCreated { get; init; }

    public bool IsDeployedFrom(NuGetVersion version)
    {
        if (Version.Major != version.Major) return false;
        if (Version.Minor != version.Minor) return false;
        var trimmedBuild = Version.Build / 100;
        return trimmedBuild == version.Patch;
    }
}
