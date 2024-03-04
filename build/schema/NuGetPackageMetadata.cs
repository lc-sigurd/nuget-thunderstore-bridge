/*
 * This file is largely based upon
 * https://github.com/Lordfirespeed/NuGet-GameLib-Dehumidifier/blob/20ec05e222b60cee6d6411116a1df5f42ee5d874/build/schema/NuGetPackageIndex.cs
 * Copyright (c) 2024 Joe Clack
 * Joe Clack licenses the referenced file to the Sigurd Team under the GPL-3.0-OR-LATER license.
 *
 * Copyright (c) 2024 Sigurd Team
 * The Sigurd Team licenses this file to you under the GPL-3.0-OR-LATER license.
 */

using System;
using System.Text.Json.Serialization;
using NuGet.Versioning;

namespace Build.Schema;

public class NuGetPackageMetadata
{
    [JsonPropertyName("items")]
    public NuGetPackageVersionPage[] VersionPages { get; set; } = null!;
}

public class NuGetPackageVersionPage
{
    [JsonPropertyName("items")]
    public NuGetPackageVersion[] Versions { get; set; } = null!;
}

public class NuGetPackageVersion : IEquatable<NuGetPackageVersion>
{
    [JsonPropertyName("catalogEntry")]
    public NuGetCatalogEntry CatalogEntry { get; set; } = null!;

    public bool Equals(NuGetPackageVersion? other)
    {
        return CatalogEntry.Equals(other?.CatalogEntry);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((NuGetPackageVersion)obj);
    }

    public override int GetHashCode()
    {
        return CatalogEntry.GetHashCode();
    }
}

public class NuGetCatalogEntry: IEquatable<NuGetCatalogEntry>
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("version")]
    public string Version { get; set; } = null!;

    [JsonPropertyName("dependencyGroups")]
    public NuGetDependencyGroup[] DependencyGroups { get; set; } = null!;

    public bool Equals(NuGetCatalogEntry? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id && Version == other.Version;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((NuGetCatalogEntry)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Version);
    }
}

public class NuGetDependencyGroup
{
    [JsonPropertyName("targetFramework")]
    public string TargetFramework { get; set; } = null!;

    [JsonPropertyName("dependencies")]
    public NuGetDependency[]? Dependencies { get; set; }
}

public class NuGetDependency
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("range")]
    public VersionRange Range { get; set; } = null!;
}
