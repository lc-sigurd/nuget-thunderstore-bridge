using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace Build.Schema.Thunderstore;

[PrimaryKey(nameof(PackageUuid))]
[Index(nameof(Namespace))]
[Index(nameof(Name))]
[Index(nameof(Namespace), nameof(Name), IsUnique = true)]
public class ThunderstorePackage
{
    [JsonPropertyName("uuid4")]
    [JsonRequired]
    public required Guid PackageUuid { get; init; }

    [JsonPropertyName("name")]
    [JsonRequired]
    [MaxLength(128)]
    public required string Name { get; init; }

    [JsonPropertyName("owner")]
    [JsonRequired]
    [MaxLength(128)]
    public required string Namespace { get; init; }

    [JsonPropertyName("versions")]
    [JsonRequired]
    public required List<ThunderstorePackageVersion> Versions { get; init; }

    [JsonIgnore]
    [NotMapped]
    private ThunderstorePackageVersion? _latestVersion;

    [JsonIgnore]
    [NotMapped]
    public ThunderstorePackageVersion LatestVersion => _latestVersion ??= ComputeLatestVersion();

    private ThunderstorePackageVersion ComputeLatestVersion() => Versions
        .MaxBy(versionListing => versionListing.VersionNumber)!;
}
