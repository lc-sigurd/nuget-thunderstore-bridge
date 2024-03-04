/*
 * Copyright (c) 2024 Sigurd Team
 * The Sigurd Team licenses this file to you under the GPL-3.0-OR-LATER license.
 */

using System;
using System.Diagnostics.CodeAnalysis;
using Build.Schema;
using Cake.Common;
using Cake.Common.IO;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Frosting;

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
    public string NuGetApiKey { get; }
    public string ThunderstoreCommunitySlug { get; }

    private CommunityConfiguration? _communityConfiguration;

    public CommunityConfiguration CommunityConfiguration
    {
        get => _communityConfiguration ?? throw new InvalidOperationException();
        set => _communityConfiguration = value;
    }

    public DirectoryPath RootDirectory { get; }
    public DirectoryPath OutputDirectory { get; }

    public BuildContext(ICakeContext context)
        : base(context)
    {
        NuGetApiKey = context.Argument<string>("nuget-api-key");
        ThunderstoreCommunitySlug = context.Argument<string>("community");

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
