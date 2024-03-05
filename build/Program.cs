/*
 * This file is largely based upon
 * https://github.com/Lordfirespeed/NuGet-GameLib-Dehumidifier/blob/20ec05e222b60cee6d6411116a1df5f42ee5d874/build/Program.cs
 * Copyright (c) 2024 Joe Clack
 * Joe Clack licenses the referenced file to the Sigurd Team under the GPL-3.0-OR-LATER license.
 *
 * Copyright (c) 2024 Sigurd Team
 * The Sigurd Team licenses this file to you under the GPL-3.0-OR-LATER license.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Build.Schema.Converters;
using Build.Schema.Local;
using Build.Schema.Thunderstore.API;
using Cake.Common;
using Cake.Common.IO;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Frosting;
using Cake.Git;
using Json.Schema.Serialization;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Build;

public static class Program
{
    public static int Main(string[] args)
    {
        return new CakeHost()
            .UseContext<BuildContext>()
            .Run(args);
    }
}

public class BuildContext : FrostingContext
{
    public string? ThunderstoreApiKey { get; }
    public string ThunderstoreCommunitySlug { get; }

    private CommunityConfiguration? _communityConfiguration;

    public CommunityConfiguration CommunityConfiguration
    {
        get => _communityConfiguration ?? throw new InvalidOperationException();
        set => _communityConfiguration = value;
    }

    private ReadOnlyCollection<PackageConfiguration>? _packageConfigurations;

    public IList<PackageConfiguration> PackageConfigurations
    {
        get => _packageConfigurations ?? throw new InvalidOperationException();
        set => _packageConfigurations = new ReadOnlyCollection<PackageConfiguration>(value);
    }

    private ReadOnlyCollection<IPackageSearchMetadata>? _packageVersionsToBridge;

    public IList<IPackageSearchMetadata> PackageVersionsToBridge {
        get => _packageVersionsToBridge ?? throw new InvalidOperationException();
        set => _packageVersionsToBridge = new ReadOnlyCollection<IPackageSearchMetadata>(value);
    }

    private ReadOnlyDictionary<(string @namespace, string name), ThunderstorePackageListing> _thunderstorePackageListingIndex;

    public IDictionary<(string @namespace, string name), ThunderstorePackageListing> ThunderstorePackageListingIndex {
        get => _thunderstorePackageListingIndex ?? throw new InvalidOperationException();
        set => _thunderstorePackageListingIndex = new ReadOnlyDictionary<(string @namespace, string name), ThunderstorePackageListing>(value);
    }

    private ReadOnlyDictionary<PackageIdentity, DownloadResourceResult>? _nuGetPackageDownloadResults;

    public IDictionary<PackageIdentity, DownloadResourceResult> NuGetPackageDownloadResults {
        get => _nuGetPackageDownloadResults ?? throw new InvalidOperationException();
        set => _nuGetPackageDownloadResults = new ReadOnlyDictionary<PackageIdentity, DownloadResourceResult>(value);
    }

    public GitCommit CurrentCommit { get; }

    public DirectoryPath RootDirectory { get; }
    public DirectoryPath IntermediateOutputDirectory { get; }
    public DirectoryPath OutputDirectory { get; }
    public DirectoryPath DistDirectory { get; }
    public DirectoryPath CommunityConfigurationsDirectory => RootDirectory.Combine("Communities");
    public DirectoryPath PackageConfigurationsDirectory => RootDirectory.Combine("Packages");

    public BuildContext(ICakeContext context)
        : base(context)
    {
        ThunderstoreApiKey = context.Argument<string?>("thunderstore-api-key", null);
        ThunderstoreCommunitySlug = context.Argument<string>("community");

        RootDirectory = context.Environment.WorkingDirectory.GetParent();
        IntermediateOutputDirectory = context.Environment.WorkingDirectory.Combine("bin");
        OutputDirectory = context.Environment.WorkingDirectory.Combine("obj");
        DistDirectory = context.Environment.WorkingDirectory.Combine("dist");

        CurrentCommit = context.GitLogTip(RootDirectory);
    }
}

[TaskName("Clean")]
public sealed class CleanTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Log.Information("Cleaning up old build artifacts...");
        context.CleanDirectories(context.DistDirectory.FullPath);
    }
}

[TaskName("Register JSON Schemas")]
public sealed class RegisterJsonSchemasTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context) { }
}

[TaskName("Deserialize configuration")]
[IsDependentOn(typeof(RegisterJsonSchemasTask))]
public sealed class DeserializeConfigurationTask : AsyncFrostingTask<BuildContext>
{
    private static JsonSerializerOptions JsonSerializerOptions = new() {
        Converters = {
            new ValidatingJsonConverter()
        },
        WriteIndented = true,
    };

    public async Task<CommunityConfiguration> DeserializeCommunityConfiguration(BuildContext context)
    {
        var communityConfigurationPath = context.CommunityConfigurationsDirectory.CombineWithFilePath($"{context.ThunderstoreCommunitySlug}.json");
        context.Log.Information($"Deserializing community configuration from {communityConfigurationPath}");
        await using FileStream communityConfigurationStream = File.OpenRead(communityConfigurationPath.FullPath);

        return await JsonSerializer.DeserializeAsync<CommunityConfiguration>(communityConfigurationStream, JsonSerializerOptions)
            ?? throw new InvalidOperationException($"Community configuration at {communityConfigurationPath} could not be deserialized.");
    }

    public async Task<PackageConfiguration> DeserializePackageConfiguration(BuildContext context, FilePath packageConfigurationPath)
    {
        context.Log.Information($"Deserializing package configuration from {packageConfigurationPath}");
        await using FileStream packageConfigurationStream = File.OpenRead(packageConfigurationPath.FullPath);

        return await JsonSerializer.DeserializeAsync<PackageConfiguration>(packageConfigurationStream, JsonSerializerOptions)
            ?? throw new InvalidOperationException($"Package configuration at {packageConfigurationPath} could not be deserialized.");
    }

    public async Task<IList<PackageConfiguration>> DeserializeAllPackageConfigurations(BuildContext context)
    {
        return await Task.WhenAll(
            context.GetFiles(new GlobPattern(context.PackageConfigurationsDirectory.CombineWithFilePath("*.json").FullPath))
                .Select(DeserializePackageConfiguration)
        );

        // ReSharper disable once LocalFunctionHidesMethod
        async Task<PackageConfiguration> DeserializePackageConfiguration(FilePath packageConfigurationPath)
            => await this.DeserializePackageConfiguration(context, packageConfigurationPath);
    }

    public override async Task RunAsync(BuildContext context)
    {
        await Task.WhenAll([
            DeserializeAndSetCommunityConfiguration(),
            DeserializeAndSetPackageConfigurations()
        ]);

        async Task DeserializeAndSetCommunityConfiguration()
        {
            context.CommunityConfiguration = await DeserializeCommunityConfiguration(context);
        }

        async Task DeserializeAndSetPackageConfigurations()
        {
            context.PackageConfigurations = await DeserializeAllPackageConfigurations(context);
        }
    }
}

[TaskName("Prepare")]
[IsDependentOn(typeof(CleanTask))]
[IsDependentOn(typeof(DeserializeConfigurationTask))]
public sealed class PrepareTask : FrostingTask;

public abstract class NuGetTaskBase : AsyncFrostingTask<BuildContext>
{
    protected static readonly SourceCacheContext SourceCache = new SourceCacheContext();
    protected static readonly PackageSource Source = new PackageSource("https://api.nuget.org/v3/index.json");
    protected static readonly SourceRepository SourceRepository = Repository.Factory.GetCoreV3(Source);
    protected static readonly IEqualityComparer<IPackageSearchMetadata> PackageSearchMetadataComparer = new PackageSearchMetadataComparerImpl();

    private class PackageSearchMetadataComparerImpl : IEqualityComparer<IPackageSearchMetadata>
    {
        public bool Equals(IPackageSearchMetadata? x, IPackageSearchMetadata? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return Equals(x.Identity, y.Identity);
        }

        public int GetHashCode(IPackageSearchMetadata obj)
        {
            return (obj.Identity != null ? obj.Identity.GetHashCode() : 0);
        }
    }
}

[TaskName("Fetch NuGet context")]
[IsDependentOn(typeof(PrepareTask))]
public sealed class FetchNuGetContextTask : NuGetTaskBase
{
    private PackageMetadataResource _packageMetadataResource = null!;
    private readonly FloatRange _absoluteLatestFloatRange = new FloatRange(NuGetVersionFloatBehavior.AbsoluteLatest);

    private async Task<IPackageSearchMetadata[]> FetchNuGetPackageMetadata(BuildContext context, string packageId)
    {
        context.Log.Information($"Fetching index for NuGet package '{packageId}'");
        return (await _packageMetadataResource.GetMetadataAsync(packageId, false, false, SourceCache, NullLogger.Instance, default))
            .ToArray();
    }

    private async Task<IList<ValueTuple<IPackageSearchMetadata[], VersionRange>>> GetNuGetPackageMetadataForDependenciesOf(BuildContext context, IPackageSearchMetadata nuGetPackageVersion, NuGetFramework framework)
    {
        var nearestDependencySet = NuGetFrameworkUtility
            .GetNearest(nuGetPackageVersion.DependencySets, framework, group => group.TargetFramework);
        var dependenciesForFramework = nearestDependencySet?.Packages ?? Array.Empty<PackageDependency>();

        return await Task.WhenAll(
            dependenciesForFramework
                .Select(async dependency => new ValueTuple<IPackageSearchMetadata[], VersionRange>(await FetchNuGetPackageMetadata(context, dependency.Id), dependency.VersionRange))
        );
    }

    private async Task<IEnumerable<IPackageSearchMetadata>> RecursivelyGetDependencyPackageVersionsOf(BuildContext context, IPackageSearchMetadata nuGetPackageVersion, NuGetFramework framework)
    {
        var dependenciesWithRanges = await GetNuGetPackageMetadataForDependenciesOf(context, nuGetPackageVersion, framework);

        var dependencyVersions = dependenciesWithRanges
            .Select(item => ResolveBestMatch(item.Item1, new VersionRange(item.Item2, _absoluteLatestFloatRange)))
            .ToArray();

        var dependenciesOfDependencyVersions = await Task.WhenAll(
            dependencyVersions
                .Select(async version => await RecursivelyGetDependencyPackageVersionsOf(context, version, framework))
        );

        return dependencyVersions
            .Concat(dependenciesOfDependencyVersions.SelectMany(x => x))
            .ToHashSet(PackageSearchMetadataComparer);
    }

    private IPackageSearchMetadata ResolveBestMatch(IPackageSearchMetadata[] packageVersions, VersionRange range)
    {
        var versionIndex = packageVersions
            .ToDictionary(version => version.Identity.Version);

        return versionIndex[range.FindBestMatch(versionIndex.Keys)!];
    }

    public override async Task RunAsync(BuildContext context)
    {
        _packageMetadataResource = await SourceRepository.GetResourceAsync<PackageMetadataResource>();

        var initialPackagesFlatDependencies = await Task.WhenAll(
            context.PackageConfigurations.Select(async package => await GetFlattenedNuGetPackageDependencies(package.PackageId))
        );

        context.PackageVersionsToBridge = initialPackagesFlatDependencies
            .SelectMany(x => x)
            .ToHashSet(PackageSearchMetadataComparer)
            .ToList();

        async Task<IEnumerable<IPackageSearchMetadata>> GetFlattenedNuGetPackageDependencies(string packageId)
        {
            var packageVersions = await FetchNuGetPackageMetadata(context, packageId);
            var latestPackageVersion = ResolveBestMatch(packageVersions, new VersionRange(new NuGetVersion("0.0.0"), _absoluteLatestFloatRange));
            var dependencies = await RecursivelyGetDependencyPackageVersionsOf(context, latestPackageVersion, context.CommunityConfiguration.RuntimeFramework);
            return dependencies.Prepend(latestPackageVersion);
        }
    }
}

[TaskName("Fetch Thunderstore context")]
[IsDependentOn(typeof(PrepareTask))]
public sealed class FetchThunderstoreContextTask : AsyncFrostingTask<BuildContext>
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() {
        Converters = {
            new ThunderstorePackageIndexJsonConverter(),
        },
    };

    private static readonly HttpClientHandler GzipHandler = new()
    {
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
    };

    private static readonly HttpClient GzipThunderstoreClient = new(GzipHandler)
    {
        BaseAddress = new Uri("https://thunderstore.io"),
    };

    private async Task<ThunderstorePackageListingIndex> FetchThunderstorePackageIndex(BuildContext context)
    {
        var response = await GzipThunderstoreClient.GetAsync($"/c/{context.ThunderstoreCommunitySlug}/api/v1/package/");
        if (response is not { IsSuccessStatusCode: true }) throw new Exception("Failed to fetch Thunderstore package index.");

        var packageMetadata = await response.Content.ReadFromJsonAsync<ThunderstorePackageListingIndex>(JsonSerializerOptions);
        if (packageMetadata is null) throw new Exception("Failed to deserialize Thunderstore package index.");

        return packageMetadata;
    }

    public override async Task RunAsync(BuildContext context)
    {
        context.ThunderstorePackageListingIndex = await FetchThunderstorePackageIndex(context);
    }
}

[TaskName("Check Thunderstore packages are up-to-date")]
[IsDependentOn(typeof(PrepareTask))]
[IsDependentOn(typeof(FetchNuGetContextTask))]
[IsDependentOn(typeof(FetchThunderstoreContextTask))]
public sealed class CheckThunderstorePackagesUpToDateTask : AsyncFrostingTask<BuildContext>
{

}

[TaskName("Download NuGet packages")]
[IsDependentOn(typeof(PrepareTask))]
[IsDependentOn(typeof(FetchNuGetContextTask))]
[IsDependentOn(typeof(FetchThunderstoreContextTask))]
[IsDependentOn(typeof(CheckThunderstorePackagesUpToDateTask))]
public sealed class DownloadNuGetPackagesTask : AsyncFrostingTask<BuildContext>
{

}

[TaskName("Resolve runtime assemblies")]
[IsDependentOn(typeof(PrepareTask))]
[IsDependentOn(typeof(DownloadNuGetPackagesTask))]
public sealed class ResolveRuntimeAssembliesTask : AsyncFrostingTask<BuildContext>
{

}

[TaskName("Serialize Thunderstore package meta-schemas")]
[IsDependentOn(typeof(PrepareTask))]
[IsDependentOn(typeof(ResolveRuntimeAssembliesTask))]
public sealed class SerializeThunderstoreMetaSchemasTask : AsyncFrostingTask<BuildContext>
{

}

[TaskName("Build Thunderstore packages")]
[IsDependentOn(typeof(PrepareTask))]
[IsDependentOn(typeof(SerializeThunderstoreMetaSchemasTask))]
public sealed class BuildThunderstorePackages : AsyncFrostingTask<BuildContext>
{

}

[TaskName("Publish built Thunderstore packages")]
public sealed class PublishThunderstorePackages : AsyncFrostingTask<BuildContext>
{

}

[TaskName("Default")]
[IsDependentOn(typeof(BuildThunderstorePackages))]
public class DefaultTask : FrostingTask { }
