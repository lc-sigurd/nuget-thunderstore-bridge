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
using System.Text.RegularExpressions;
using System.Threading;
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
using ImageMagick;
using Json.Schema.Serialization;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Packaging.Licenses;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using ThunderstoreCLI.Commands;
using ThunderstoreCLI.Configuration;
using ThunderstoreCLI.Models;
using Utils;

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
    public string ThunderstoreCommunitySlug { get; }

    public Versioner Versioner;

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

    private ReadOnlyCollection<IPackageSearchMetadata>? _initialPackageVersionsToBridge;

    public IList<IPackageSearchMetadata> InitialPackageVersionsToBridge {
        get => _initialPackageVersionsToBridge ?? throw new InvalidOperationException();
        set => _initialPackageVersionsToBridge = new ReadOnlyCollection<IPackageSearchMetadata>(value);
    }

    private ReadOnlyCollection<IPackageSearchMetadata>? _allPackageVersionsToBridge;

    public IList<IPackageSearchMetadata> AllPackageVersionsToBridge {
        get => _allPackageVersionsToBridge ?? throw new InvalidOperationException();
        set => _allPackageVersionsToBridge = new ReadOnlyCollection<IPackageSearchMetadata>(value);
    }

    private ReadOnlyDictionary<PackageIdentity, IList<PackageIdentity>>? _resolvedPackageVersionDependencies;

    public IDictionary<PackageIdentity, IList<PackageIdentity>> ResolvedPackageVersionDependencies {
        get => _resolvedPackageVersionDependencies ?? throw new InvalidOperationException();
        set => _resolvedPackageVersionDependencies = new ReadOnlyDictionary<PackageIdentity, IList<PackageIdentity>>(value);
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

    private ReadOnlyDictionary<PackageIdentity, ThunderstoreProject> _thunderstoreMetaSchemas;

    public IDictionary<PackageIdentity, ThunderstoreProject> ThunderstoreMetaSchemas {
        get => _thunderstoreMetaSchemas ?? throw new InvalidOperationException();
        set => _thunderstoreMetaSchemas = new ReadOnlyDictionary<PackageIdentity, ThunderstoreProject>(value);
    }

    public ThunderstorePackageListing GetThunderstoreListing(PackageIdentity identity) => ThunderstorePackageListingIndex[(CommunityConfiguration.PackageNamespace, identity.Id)];

    private Dictionary<PackageIdentity, Version> _nextFreeVersionCache = new();

    private Version ComputeNextFreeVersion(PackageIdentity identity)
    {
        var versionNumber = new Version(identity.Version.Major, identity.Version.Minor, identity.Version.Patch * 100);
        ThunderstorePackageListing thunderstorePackage;
        try {
            thunderstorePackage = GetThunderstoreListing(identity);
        }
        catch (KeyNotFoundException) {
            return versionNumber;
        }

        while (thunderstorePackage.Versions.ContainsKey(versionNumber)) {
            versionNumber = new Version(versionNumber.Major, versionNumber.Minor, versionNumber.Build + 1);
            if (versionNumber.Build % 100 == 0) throw new InvalidOperationException("Too many versions!!");
        }

        return versionNumber;
    }

    public Version GetNextFreeVersion(PackageIdentity identity)
    {
        if (_nextFreeVersionCache.TryGetValue(identity, out var nextFreeVersion)) return nextFreeVersion;
        nextFreeVersion = ComputeNextFreeVersion(identity);
        _nextFreeVersionCache[identity] = nextFreeVersion;
        return nextFreeVersion;
    }

    public PackageReaderBase GetPackageReader(PackageIdentity identity) => NuGetPackageDownloadResults[identity].PackageReader;

    public DirectoryPath GetIntermediatePackageLibSubdirectory(PackageIdentity identity) {
        var destination = IntermediatePackageLibDirectory.Combine($"{identity.Id}/{identity.Id}-{identity.Version}");
        this.EnsureDirectoryExists(destination);
        return destination;
    }

    public DirectoryPath RootDirectory { get; }
    public DirectoryPath IntermediateOutputDirectory { get; }
    public DirectoryPath OutputDirectory { get; }
    public DirectoryPath DistDirectory { get; }
    public DirectoryPath CommunityConfigurationsDirectory => RootDirectory.Combine("Communities");
    public DirectoryPath PackageConfigurationsDirectory => RootDirectory.Combine("Packages");
    public DirectoryPath IntermediatePackageLibDirectory => IntermediateOutputDirectory.Combine("package-lib");
    public FilePath FallbackIconPath => RootDirectory.CombineWithFilePath("assets/icons/nuget.png");

    public BuildContext(ICakeContext context)
        : base(context)
    {
        ThunderstoreCommunitySlug = context.Argument<string>("community");

        RootDirectory = context.Environment.WorkingDirectory.GetParent();
        IntermediateOutputDirectory = context.Environment.WorkingDirectory.Combine("obj");
        OutputDirectory = context.Environment.WorkingDirectory.Combine("bin");
        DistDirectory = context.Environment.WorkingDirectory.Combine("dist");

        Versioner = new(RootDirectory.FullPath);
    }
}

[TaskName("Clean")]
public sealed class CleanTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Log.Information("Cleaning up old build artifacts...");
        context.CleanDirectories(context.DistDirectory.FullPath);
        context.CleanDirectories(context.IntermediatePackageLibDirectory.FullPath);
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

    private Dictionary<PackageIdentity, IList<PackageIdentity>> ResolvedPackageDependencies = new();

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
            .Select(item => ResolveBestMatch(item.Item1, framework, new VersionRange(item.Item2, _absoluteLatestFloatRange)))
            .ToArray();

        var dependenciesOfDependencyVersions = await Task.WhenAll(
            dependencyVersions
                .Select(async version => await RecursivelyGetDependencyPackageVersionsOf(context, version, framework))
        );

        ResolvedPackageDependencies[nuGetPackageVersion.Identity] = dependencyVersions
            .Select(packageVersion => packageVersion.Identity)
            .ToList();

        return dependencyVersions
            .Concat(dependenciesOfDependencyVersions.SelectMany(x => x))
            .ToHashSet(PackageSearchMetadataComparer);
    }

    private IPackageSearchMetadata ResolveBestMatch(IPackageSearchMetadata[] packageVersions, NuGetFramework framework, VersionRange range)
    {
        var orderedVersionCandidates = packageVersions
            .Where(packageVersion => range.Satisfies(packageVersion.Identity.Version))
            .OrderByDescending(packageVersion => packageVersion.Identity.Version);

        return orderedVersionCandidates.First(SupportsFramework);

        bool SupportsFramework(IPackageSearchMetadata packageVersion)
            => NuGetFrameworkUtility.GetNearest(packageVersion.DependencySets, framework, group => group.TargetFramework) is not null;
    }

    public override async Task RunAsync(BuildContext context)
    {
        _packageMetadataResource = await SourceRepository.GetResourceAsync<PackageMetadataResource>();

        var initialPackagesFlatDependencies = await Task.WhenAll(
            context.PackageConfigurations.Select(async package => await GetFlattenedNuGetPackageDependencies(package.PackageId))
        );

        context.AllPackageVersionsToBridge = initialPackagesFlatDependencies
            .SelectMany(x => x)
            .ToHashSet(PackageSearchMetadataComparer)
            .ToList();

        var initialPackageIds = context.PackageConfigurations
            .Select(packageConfiguration => packageConfiguration.PackageId)
            .ToHashSet();

        context.InitialPackageVersionsToBridge = context.AllPackageVersionsToBridge
            .Where(packageVersion => initialPackageIds.Contains(packageVersion.Identity.Id))
            .ToList();

        context.ResolvedPackageVersionDependencies = ResolvedPackageDependencies;

        async Task<IEnumerable<IPackageSearchMetadata>> GetFlattenedNuGetPackageDependencies(string packageId)
        {
            var packageVersions = await FetchNuGetPackageMetadata(context, packageId);
            var latestPackageVersion = ResolveBestMatch(packageVersions, context.CommunityConfiguration.RuntimeFramework, new VersionRange(new NuGetVersion("0.0.0"), _absoluteLatestFloatRange));
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
public sealed class CheckThunderstorePackagesUpToDateTask : FrostingTask<BuildContext>
{
    private bool HasDeployedVersion(BuildContext context, IPackageSearchMetadata packageVersion)
    {
        var thunderstoreFullName = (context.CommunityConfiguration.PackageNamespace, packageVersion.Identity.Id);
        if (!context.ThunderstorePackageListingIndex.TryGetValue(thunderstoreFullName, out var thunderstoreListing)) return false;
        if (!thunderstoreListing.LatestVersion.IsDeployedFrom(packageVersion.Identity.Version)) return false;
        if (thunderstoreListing.LatestVersion.DateCreated < context.Versioner.LastVersionChangeWhen) return false;

        return true;
    }

    public override void Run(BuildContext context)
    {
        context.AllPackageVersionsToBridge = context.AllPackageVersionsToBridge
            .Where(packageVersion => !HasDeployedVersion(context, packageVersion))
            .ToList();
    }
}

[TaskName("Download NuGet packages")]
[IsDependentOn(typeof(PrepareTask))]
[IsDependentOn(typeof(FetchNuGetContextTask))]
[IsDependentOn(typeof(FetchThunderstoreContextTask))]
[IsDependentOn(typeof(CheckThunderstorePackagesUpToDateTask))]
public sealed class DownloadNuGetPackagesTask : NuGetTaskBase
{
    private static readonly PackageDownloadContext PackageDownloadContext = new(SourceCache);
    private static NuGetPathContext _pathContext = null!;
    private static DownloadResource _downloadResource = null!;

    public override bool ShouldRun(BuildContext context)
    {
        if (context.AllPackageVersionsToBridge.Count == 0) return false;
        return base.ShouldRun(context);
    }

    private async Task<DownloadResourceResult> DownloadNuGetPackageVersion(BuildContext context, PackageIdentity packageIdentity)
    {
        return await _downloadResource.GetDownloadResourceResultAsync(
            packageIdentity,
            PackageDownloadContext,
            _pathContext.UserPackageFolder,
            NullLogger.Instance,
            default
        );
    }

    public override async Task RunAsync(BuildContext context)
    {
        _pathContext = NuGetPathContext.Create(context.RootDirectory.FullPath);
        _downloadResource = await SourceRepository.GetResourceAsync<DownloadResource>();

        var downloadResults = await Task.WhenAll(
            context.AllPackageVersionsToBridge
                .Select(packageVersion => DownloadNuGetPackageVersion(context, packageVersion.Identity))
        );

        context.NuGetPackageDownloadResults = downloadResults
            .ToDictionary(result => result.PackageReader.GetIdentity());
    }
}

[TaskName("Extract package assets")]
[IsDependentOn(typeof(PrepareTask))]
[IsDependentOn(typeof(DownloadNuGetPackagesTask))]
public sealed class ExtractNuGetPackageAssetsTask : AsyncFrostingTask<BuildContext>
{
    private static readonly HashSet<string> WhitelistedLibFileExtensions = [".dll", ".pdb"];

    private static readonly HttpClient Client = new();
    private static readonly Regex GitHubRichFileViewerUrl = new("(https?://github\\.com/.*)/(?:blob|tree)/(.*)", RegexOptions.Compiled);

    private static readonly MagickGeometry IconSize = new(256, 256) {
        IgnoreAspectRatio = true,
    };

    public override bool ShouldRun(BuildContext context)
    {
        if (context.AllPackageVersionsToBridge.Count == 0) return false;
        return base.ShouldRun(context);
    }

    private DirectoryPath GetPackageDestination(BuildContext context, PackageIdentity identity) => context.GetIntermediatePackageLibSubdirectory(identity);


    private async Task<IEnumerable<string>> ExtractPackageItems(BuildContext context, PackageIdentity identity, string[] items)
    {
        var destination = GetPackageDestination(context, identity);

        var packageFileExtractor = new PackageFileExtractor(items, XmlDocFileSaveMode.Skip);

        return await context
            .GetPackageReader(identity)
            .CopyFilesAsync(
                destination.FullPath,
                items,
                packageFileExtractor.ExtractPackageFile,
                NullLogger.Instance,
                default
            );
    }

    private async Task<IEnumerable<string>> FindLibItems(BuildContext context, PackageIdentity identity)
    {
        var libItemsGroupPerFramework = await context
            .GetPackageReader(identity)
            .GetLibItemsAsync(default);
        if (libItemsGroupPerFramework is null) return Enumerable.Empty<string>();
        var libItemsGroupForNearestFramework = NuGetFrameworkUtility.GetNearest(libItemsGroupPerFramework, context.CommunityConfiguration.RuntimeFramework, group => group.TargetFramework)!;
        return libItemsGroupForNearestFramework.Items
            .Where(HasWhitelistedExtension);

        bool HasWhitelistedExtension(string itemPath) => WhitelistedLibFileExtensions.Contains(System.IO.Path.GetExtension(itemPath));
    }

    private async Task FetchUri(BuildContext context, Uri uri, FilePath destination)
    {
        var maybeMatch = GitHubRichFileViewerUrl.Match(uri.ToString());
        if (maybeMatch is { Success: true }) {
            uri = new Uri($"{maybeMatch.Groups[1]}/raw/{maybeMatch.Groups[2]}");
        }

        var response = await Client.GetAsync(uri);
        if (response is not { IsSuccessStatusCode: true }) throw new Exception($"Failed to fetch {uri}");

        var responseContent = await response.Content.ReadAsStreamAsync();
        await using FileStream stream = File.OpenWrite(destination.FullPath);
        await responseContent.CopyToAsync(stream);
    }

    private async Task FetchLicense(BuildContext context, NuGetLicense license, FilePath destination)
    {
        await FetchUri(context, new Uri($"https://github.com/spdx/license-list-data/raw/main/text/{license.Identifier}.txt"), destination);
    }

    private async Task FetchLicensesInExpression(BuildContext context, NuGetLicenseExpression expression, DirectoryPath destination)
    {
        if (expression is WithOperator { Type: LicenseExpressionType.Operator } withOperator) {
            await FetchLicensesInExpression(context, withOperator.License, destination);
            return;
        }

        if (expression is LogicalOperator { Type: LicenseExpressionType.Operator } logicalOperator) {
            await Task.WhenAll(
                FetchLicensesInExpression(context, logicalOperator.Left, destination),
                FetchLicensesInExpression(context, logicalOperator.Right, destination)
            );
            return;
        }

        if (expression is NuGetLicense { Type: LicenseExpressionType.License } license) {
            var licensePath = destination.CombineWithFilePath($"{license.Identifier}.txt");
            await FetchLicense(context, license, licensePath);
            return;
        }

        throw new ArgumentOutOfRangeException(nameof(expression.Type), $"Unrecognised {nameof(LicenseExpressionType)}");
    }

    private async Task CopyLicense(BuildContext context, PackageIdentity identity)
    {
        var reader = context.GetPackageReader(identity);
        var licenseMetadata = reader.NuspecReader.GetLicenseMetadata();
        var licenseUrl = reader.NuspecReader.GetLicenseUrl();

        var packageDestination = GetPackageDestination(context, identity);
        var mainLicenseFilePath = packageDestination.CombineWithFilePath("LICENSE");

        if (licenseMetadata is { Type: LicenseType.Expression, LicenseExpression.Type: LicenseExpressionType.Operator }) {
            await FetchLicensesInExpression(context, licenseMetadata.LicenseExpression, packageDestination.Combine("licenses"));
            await using FileStream stream = File.OpenWrite(mainLicenseFilePath.FullPath);
            await using StreamWriter writer = new StreamWriter(stream);
            await writer.WriteAsync(licenseMetadata.LicenseExpression.ToString());
            return;
        }

        if (licenseMetadata is { Type: LicenseType.Expression, LicenseExpression.Type: LicenseExpressionType.License }) {
            await FetchLicense(context, (NuGetLicense)licenseMetadata.LicenseExpression, mainLicenseFilePath);
            return;
        }

        if (licenseMetadata is { Type: LicenseType.File }) {
            var extractedPaths = await ExtractPackageItems(context, identity, [licenseMetadata.License]);
            var extractedLicensePath = extractedPaths.Single();
            context.MoveFile(extractedLicensePath, mainLicenseFilePath);
            return;
        }

        if (!String.IsNullOrWhiteSpace(licenseUrl)) {
            await FetchUri(context, new Uri(licenseUrl), mainLicenseFilePath);
            return;
        }
    }

    private async Task CopyReadmeFor(BuildContext context, PackageIdentity identity)
    {
        var reader = context.GetPackageReader(identity);
        var readmeItem = reader.NuspecReader.GetReadme();
        var readmePath = GetPackageDestination(context, identity).CombineWithFilePath("README.md");

        if (!String.IsNullOrWhiteSpace(readmeItem)) {
            var extractedPaths = await ExtractPackageItems(context, identity, [readmeItem]);
            var extractedReadmePath = extractedPaths.Single();
            context.MoveFile(extractedReadmePath, readmePath);
            return;
        }

        var description = GetDescription();

        await using FileStream stream = File.OpenWrite(readmePath.FullPath);
        await using StreamWriter writer = new StreamWriter(stream);

        await writer.WriteLineAsync($"# {GetTitle()}");
        if (description is not null) {
            await writer.WriteLineAsync();
            await writer.WriteAsync(description);
        }

        string GetTitle()
        {
            string title = reader.NuspecReader.GetTitle();
            if (!string.IsNullOrWhiteSpace(title)) return title;

            string id = reader.NuspecReader.GetId();
            if (!string.IsNullOrWhiteSpace(id)) return id;

            return identity.Id;
        }

        string? GetDescription()
        {
            string? description = reader.NuspecReader.GetDescription();
            if (string.IsNullOrWhiteSpace(description)) return null;
            return description;
        }
    }

    private async Task CopyIcon(BuildContext context, PackageIdentity identity)
    {
        var reader = context.GetPackageReader(identity);
        var iconRelativePath = reader.NuspecReader.GetIcon();
        var iconUrl = reader.NuspecReader.GetIconUrl();

        var packageDestination = GetPackageDestination(context, identity);
        var iconFilePath = packageDestination.CombineWithFilePath("icon.png");

        if (!string.IsNullOrWhiteSpace(iconRelativePath)) {
            var extractedPaths = await ExtractPackageItems(context, identity, [iconRelativePath]);
            var extractedIconPath = extractedPaths.Single();
            await ResizeImage(extractedIconPath);
            return;
        }

        if (!string.IsNullOrWhiteSpace(iconUrl)) {
            await FetchUri(context, new Uri(iconUrl), iconFilePath);
            await ResizeImage(iconFilePath);
            return;
        }

        context.CopyFile(context.FallbackIconPath, iconFilePath);

        async Task ResizeImage(FilePath filePath)
        {
            using var image = new MagickImage(filePath.FullPath);
            image.Resize(IconSize);
            context.DeleteFile(filePath);
            await image.WriteAsync(iconFilePath.FullPath);
        }
    }

    private async Task CopyLibItems(BuildContext context, PackageIdentity identity)
    {
        var itemRelativePaths = (await FindLibItems(context, identity)).ToArray();
        if (itemRelativePaths.Length == 0) return;

        await ExtractPackageItems(context, identity, itemRelativePaths);

        var packageDestination = GetPackageDestination(context, identity);

        var libItemsSource = packageDestination.Combine("lib/");
        var libItemsDestination = packageDestination.Combine($"BepInEx/core/{identity.Id}");
        if (context.DirectoryExists(libItemsDestination)) context.DeleteDirectory(libItemsDestination, new() { Recursive = true });
        context.CopyDirectory(libItemsSource, libItemsDestination);
        context.DeleteDirectory(libItemsSource,  new() { Recursive = true });
    }

    private async Task CopyAllItems(BuildContext context, PackageIdentity identity)
    {
        await Task.WhenAll(
            CopyLicense(context, identity),
            CopyReadmeFor(context, identity),
            CopyIcon(context, identity),
            CopyLibItems(context, identity)
        );

        RemoveEmptyDirectories(new DirectoryInfo(GetPackageDestination(context, identity).FullPath));

        void RemoveEmptyDirectories(DirectoryInfo dir)
        {
            foreach (var subDir in dir.GetDirectories()) {
                RemoveEmptyDirectories(subDir);
            }

            if (dir.GetFileSystemInfos().Length == 0) {
                dir.Delete();
            }
        }
    }

    public override async Task RunAsync(BuildContext context)
    {
        await Task.WhenAll(
            context.AllPackageVersionsToBridge.Select(packageVersion => CopyAllItems(context, packageVersion.Identity))
        );
    }
}

[TaskName("Construct Thunderstore package meta-schemas")]
[IsDependentOn(typeof(PrepareTask))]
[IsDependentOn(typeof(ExtractNuGetPackageAssetsTask))]
public sealed class ConstructThunderstoreMetaSchemasTask : AsyncFrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        if (context.AllPackageVersionsToBridge.Count == 0) return false;
        return base.ShouldRun(context);
    }

    private string FormatPackageName(string packageId)
    {
        return packageId
            .Replace(".", "_")
            .Replace("-", "_");
    }

    private Version GetVersionOfLatestDeploy(BuildContext context, PackageIdentity identity)
    {
        var deployingNow = context.AllPackageVersionsToBridge
            .Select(package => package.Identity)
            .FirstOrDefault(checkIdentity => Equals(checkIdentity, identity));

        if (deployingNow is not null) {
            return context.GetNextFreeVersion(deployingNow);
        }

        try {
            return context.GetThunderstoreListing(identity)
                .Versions
                .Values
                .Where(versionListing => versionListing.IsDeployedFrom(identity.Version))
                .MaxBy(versionListing => versionListing.Version)!
                .Version;
        }
        catch (KeyNotFoundException) {
            return context.GetNextFreeVersion(identity);
        }
    }

    private Dictionary<string, string> ComputeDependenciesFor(BuildContext context, IPackageSearchMetadata packageVersion)
    {
        if (!context.ResolvedPackageVersionDependencies.TryGetValue(packageVersion.Identity, out var resolvedDependencies)) return new();

        return resolvedDependencies
            .ToDictionary(
                dependencyIdentity => $"{context.CommunityConfiguration.PackageNamespace}-{FormatPackageName(dependencyIdentity.Id)}",
                dependencyIdentity => GetVersionOfLatestDeploy(context, dependencyIdentity).ToString()
            );
    }

    private ThunderstoreProject.BuildData.CopyPath[] ComputeCopyPathsFor(BuildContext context, IPackageSearchMetadata packageVersion)
    {
        return [
            new() {
                Source = "./LICENSE",
                Target = "/",
            },
            new() {
                Source = "./BepInEx",
                Target = "/BepInEx",
            },
        ];
    }

    private Task<ThunderstoreProject> ConstructThunderstoreMetaSchemaFor(BuildContext context, IPackageSearchMetadata packageVersion)
    {
        var identity = packageVersion.Identity;
        var packageLibSubDir = context.GetIntermediatePackageLibSubdirectory(identity);

        return Task.FromResult(new ThunderstoreProject {
            Package = new() {
                Namespace = context.CommunityConfiguration.PackageNamespace,
                Name = FormatPackageName(identity.Id),
                VersionNumber = context.GetNextFreeVersion(identity).ToString(),
                Description = $"NuGet {identity.Id} package re-bundled for convenient consumption and dependency management.",
                WebsiteUrl = $"https://nuget.org/packages/{identity.Id}/{identity.Version}",
                ContainsNsfwContent = false,
                Dependencies = ComputeDependenciesFor(context, packageVersion),
            },
            Build = new() {
                Icon = packageLibSubDir
                    .GetRelativePath(packageLibSubDir.Combine("icon.png"))
                    .FullPath,
                OutDir = packageLibSubDir
                    .GetRelativePath(context.DistDirectory)
                    .FullPath,
                Readme = packageLibSubDir
                    .GetRelativePath(packageLibSubDir.Combine("README.md"))
                    .FullPath,
                CopyPaths = ComputeCopyPathsFor(context, packageVersion),
            },
            Publish = new() {
                Categories = new ThunderstoreProject.CategoryDictionary {
                    Categories = new() {
                        { context.CommunityConfiguration.CommunitySlug, ["misc"] }
                    }
                },
                Communities = [ context.CommunityConfiguration.CommunitySlug ],
                Repository = Config.DefaultConfig.GeneralConfig.Repository,
            },
            Install = new() {
                InstallerDeclarations = [new() { Identifier = "foo-installer" }],
            },
        });
    }

    public override async Task RunAsync(BuildContext context)
    {
        var metaSchemas = await Task.WhenAll(
            context.AllPackageVersionsToBridge
                .Select(async packageVersion => await ConstructThunderstoreMetaSchemaFor(context, packageVersion))
        );

        context.ThunderstoreMetaSchemas = context.AllPackageVersionsToBridge
            .Zip(metaSchemas)
            .ToDictionary(pair => pair.Item1.Identity, pair => pair.Item2);
    }
}

[TaskName("Build Thunderstore packages")]
[IsDependentOn(typeof(PrepareTask))]
[IsDependentOn(typeof(ConstructThunderstoreMetaSchemasTask))]
public sealed class BuildThunderstorePackagesTask : AsyncFrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        if (context.AllPackageVersionsToBridge.Count == 0) return false;
        return base.ShouldRun(context);
    }

    private async Task BuildThunderstorePackage(BuildContext context, PackageIdentity identity)
    {
        var metaSchema = context.ThunderstoreMetaSchemas[identity];
        var metaSchemaConfigProvider = new ProjectConfig {
            Project = metaSchema,
            ProjectPath = new DirectoryInfo(context.GetIntermediatePackageLibSubdirectory(identity).FullPath),
        };

        var config = Config.Parse([new EnvironmentConfig(), metaSchemaConfigProvider]);
        BuildCommand.DoBuild(config);
    }

    public override async Task RunAsync(BuildContext context)
    {
        await Task.WhenAll(
            context.ThunderstoreMetaSchemas.Keys
                .Select(BuildThunderstorePackage)
        );

        async Task BuildThunderstorePackage(PackageIdentity identity) => await this.BuildThunderstorePackage(context, identity);
    }
}

[TaskName("Publish built Thunderstore packages")]
[IsDependentOn(typeof(BuildThunderstorePackagesTask))]
public sealed class PublishThunderstorePackagesTask : AsyncFrostingTask<BuildContext>
{
    private readonly HashSet<PackageIdentity> _attemptedDeploys = new();
    private static SemaphoreSlim _attemptDeployLock = new(1, 1);

    public override bool ShouldRun(BuildContext context)
    {
        if (context.AllPackageVersionsToBridge.Count == 0) return false;
        return base.ShouldRun(context);
    }

    private async Task PublishThunderstorePackage(BuildContext context, PackageIdentity identity)
    {
        await Task.WhenAll(
            context.ResolvedPackageVersionDependencies[identity]
                .Select(async dependencyIdentity => await PublishThunderstorePackage(context, dependencyIdentity))
        );

        if (_attemptedDeploys.Contains(identity)) return;
        await _attemptDeployLock.WaitAsync();
        try {
            if (!_attemptedDeploys.Add(identity)) return;
        }
        finally {
            _attemptDeployLock.Release();
        }

        var metaSchema = context.ThunderstoreMetaSchemas[identity];
        var metaSchemaConfigProvider = new ProjectConfig {
            Project = metaSchema,
            ProjectPath = new DirectoryInfo(context.GetIntermediatePackageLibSubdirectory(identity).FullPath),
        };

        var config = Config.Parse([new EnvironmentConfig(), metaSchemaConfigProvider]);
        PublishCommand.PublishFile(config, config.GetBuildOutputFile());
    }

    public override async Task RunAsync(BuildContext context)
    {
        await Task.WhenAll(
            context.InitialPackageVersionsToBridge
                .Select(packageVersion => packageVersion.Identity)
                .Select(async identity => await PublishThunderstorePackage(context, identity))
        );
    }
}

[TaskName("Default")]
[IsDependentOn(typeof(BuildThunderstorePackagesTask))]
public class DefaultTask : FrostingTask { }
