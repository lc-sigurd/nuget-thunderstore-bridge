/*
 * This file is largely based upon
 * https://github.com/thunderstore-io/thunderstore-cli/blob/10b73c843f2dd1a9ed9c6cb687dbbaa555626052/ThunderstoreCLI/PackageManifestV1.cs
 * thunderstore-cli Copyright (c) 2021 Thunderstore
 * Thunderstore expressly permits Lordfirespeed to use and redistribute the source of thunderstore-cli as Lordfirespeed sees fit.
 * Lordfirespeed licenses the referenced file to the Sigurd team under the MIT license.
 *
 * Copyright (c) 2024 Sigurd Team
 * The Sigurd Team licenses this file to you under the LGPL-3.0-OR-LATER license.
 */

using System;
using System.Linq;
using Newtonsoft.Json;

namespace ThunderstoreCLI.Models;

public class PackageManifestV1 : BaseJson<PackageManifestV1>
{
    [JsonProperty("namespace")]
    public string? Namespace { get; set; }

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("description")]
    public string? Description { get; set; }

    [JsonProperty("version_number")]
    public string? VersionNumber { get; set; }

    [JsonProperty("dependencies")]
    public string[]? Dependencies { get; set; }

    [JsonProperty("website_url")]
    public string? WebsiteUrl { get; set; }

    [JsonProperty("installers")]
    public InstallerDeclaration[]? Installers { get; set; }

    private string? fullName;
    [JsonIgnore]
    public string FullName => fullName ??= $"{Namespace}-{Name}";

    public class InstallerDeclaration
    {
        [JsonProperty("identifier")]
        public string? Identifier { get; set; }
    }

    public PackageManifestV1() { }

    public PackageManifestV1(PackageVersionData version)
    {
        Namespace = version.Namespace;
        Name = version.Name;
        Description = version.Description;
        VersionNumber = version.VersionNumber;
        Dependencies = version.Dependencies?.ToArray() ?? Array.Empty<string>();
        WebsiteUrl = version.WebsiteUrl;
    }

    public PackageManifestV1(PackageListingV1 listing, PackageVersionV1 version)
    {
        Namespace = listing.Owner;
        Name = listing.Name;
        Description = version.Description;
        VersionNumber = version.VersionNumber;
        Dependencies = version.Dependencies?.ToArray() ?? Array.Empty<string>();
        WebsiteUrl = version.WebsiteUrl;
    }
}
