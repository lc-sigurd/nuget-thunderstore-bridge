/*
 * Copyright (c) 2024 Sigurd Team
 * The Sigurd Team licenses this file to you under the GPL-3.0-OR-LATER license.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Build.Schema;
using Cake.Common;
using Cake.Common.IO;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Frosting;
using Json.Schema.Serialization;

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
    public string? NuGetApiKey { get; }
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

    public DirectoryPath RootDirectory { get; }
    public DirectoryPath OutputDirectory { get; }

    public DirectoryPath CommunityConfigurationsDirectory => RootDirectory.Combine("Communities");
    public DirectoryPath PackageConfigurationsDirectory => RootDirectory.Combine("Packages");

    public BuildContext(ICakeContext context)
        : base(context)
    {
        NuGetApiKey = context.Argument<string?>("nuget-api-key");
        ThunderstoreCommunitySlug = context.Argument<string?>("community") ?? throw new ArgumentNullException(nameof(ThunderstoreCommunitySlug), "Thunderstore community slug must be set.");

        RootDirectory = context.Environment.WorkingDirectory.GetParent();
        OutputDirectory = context.Environment.WorkingDirectory.Combine("dist");
    }
}

[TaskName("Clean")]
public sealed class CleanTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Log.Information("Cleaning up old build artifacts...");
        context.CleanDirectories(context.OutputDirectory.FullPath);
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
    public static JsonSerializerOptions JsonSerializerOptions = new() {
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
[IsDependentOn(typeof(RegisterJsonSchemasTask))]
[IsDependentOn(typeof(DeserializeConfigurationTask))]
public sealed class Prepare : FrostingTask { }

[TaskName("Fetch NuGet context")]
public sealed class FetchNuGetContextTask : AsyncFrostingTask<BuildContext> {

}

[TaskName("Fetch Thunderstore context")]
[IsDependentOn(typeof(FetchNuGetContextTask))]
public sealed class FetchThunderstoreContextTask : AsyncFrostingTask<BuildContext>
{

}

[TaskName("Check Thunderstore packages are up-to-date")]
[IsDependentOn(typeof(FetchThunderstoreContextTask))]
public sealed class CheckThunderstorePackagesUpToDateTask : AsyncFrostingTask<BuildContext>
{

}

[TaskName("Download NuGet packages")]
public sealed class DownloadNuGetPackagesTask : AsyncFrostingTask<BuildContext>
{

}

[TaskName("Resolve runtime assemblies")]
public sealed class ResolveRuntimeAssembliesTask : AsyncFrostingTask<BuildContext>
{

}

[TaskName("Serialize Thunderstore package meta-schemas")]
public sealed class SerializeThunderstoreMetaSchemasTask : AsyncFrostingTask<BuildContext>
{

}

[TaskName("Build Thunderstore packages")]
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
