using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using NuGet.Versioning;

namespace Build.Schema.Thunderstore;

[PrimaryKey(nameof(PackageVersionUuid))]
public class ThunderstorePackageVersion
{
    [JsonPropertyName("uuid4")]
    [JsonRequired]
    public required Guid PackageVersionUuid { get; init; }

    #region version number handling
    [JsonPropertyName("version_number")]
    [JsonRequired]
    [MaxLength(32)]
    public string VersionNumberString
    {
        get => VersionNumber.ToString();
        init
        {
            try
            {
                VersionNumber = new Version(value);
            }
            catch (Exception ex) when (false
                || ex is ArgumentException
                || ex is ArgumentNullException
                || ex is ArgumentOutOfRangeException
                || ex is FormatException
                || ex is OverflowException
            )
            { }
        }
    }

    [JsonIgnore]
    [NotMapped]
    public Version VersionNumber { get; init; } = null!;
    #endregion

    [JsonPropertyName("date_created")]
    public required DateTimeOffset DateCreated { get; init; }

    [JsonIgnore]
    public Guid? PackageUuid { get; set; }
    [JsonIgnore]
    [ForeignKey(nameof(PackageUuid))]
    public ThunderstorePackage? Package { get; set; }

    public bool IsDeployedFrom(NuGetVersion version)
    {
        if (VersionNumber.Major != version.Major) return false;
        if (VersionNumber.Minor != version.Minor) return false;
        var trimmedBuild = VersionNumber.Build / 100;
        return trimmedBuild == version.Patch;
    }
}
