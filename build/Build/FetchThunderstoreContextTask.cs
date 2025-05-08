using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Build.Schema.Converters;
using Build.Schema.Thunderstore;
using Cake.Frosting;
using Microsoft.EntityFrameworkCore;
using Utils;

namespace Build;

[TaskName("Fetch Thunderstore context")]
[IsDependentOn(typeof(PrepareTask))]
public sealed class FetchThunderstoreContextTask : AsyncFrostingTask<BuildContext>
{
    private static readonly HttpClientHandler GzipHandler = new()
    {
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
    };

    private static HttpClient GetGzipThunderstoreClient() => new(GzipHandler)
    {
        BaseAddress = new Uri("https://thunderstore.io"),
    };

    private async Task<PackageIndexContext> PopulateThunderstorePackageIndex(BuildContext context)
    {
        var dbContext = new PackageIndexContext { DbPath = $"./{context.ThunderstoreCommunitySlug}-index.db" };
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();

        using var httpClient = GetGzipThunderstoreClient();
        var stream = await httpClient.GetStreamAsync($"/c/{context.ThunderstoreCommunitySlug}/api/v1/package/");

        var enumerable = JsonSerializer.DeserializeAsyncEnumerable<ThunderstorePackage>(stream);
        var bufferer = new SaveBufferer(dbContext, 500);
        await foreach (var package in enumerable)
        {
            if (package is null)
                continue;
            foreach (var version in package.Versions.ToArray())
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (version.VersionNumber is null)
                    package.Versions.Remove(version);
            }
            dbContext.Add(package);
            await bufferer.BufferedSave();
        }
        await bufferer.Save();

        return dbContext;
    }

    public override async Task RunAsync(BuildContext context)
    {
        context.ThunderstorePackageIndexContext = await PopulateThunderstorePackageIndex(context);
    }
}
