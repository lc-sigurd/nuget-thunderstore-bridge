/*
 * This file is largely based upon
 * https://github.com/thunderstore-io/thunderstore-cli/blob/10b73c843f2dd1a9ed9c6cb687dbbaa555626052/ThunderstoreCLI/Utils/MiscUtils.cs
 * thunderstore-cli Copyright (c) 2021 Thunderstore
 * Thunderstore expressly permits Lordfirespeed to use and redistribute the source of thunderstore-cli as Lordfirespeed sees fit.
 * Lordfirespeed licenses the referenced file to the Sigurd team under the MIT license.
 *
 * Copyright (c) 2024 Sigurd Team
 * The Sigurd Team licenses this file to you under the LGPL-3.0-OR-LATER license.
 */

using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Build;

namespace ThunderstoreCLI.Utils;

public static class MiscUtils
{
    /// <summary>Return application version</summary>
    /// Version number is controlled via MinVer by creating new tags
    /// in git. See README for more information.
    public static int[] GetCurrentVersion()
    {
        string version;

        try
        {
            version = typeof(Program).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
                .InformationalVersion;
        }
        catch (NullReferenceException)
        {
            throw new Exception("Reading app version from assembly failed");
        }

        // Drop possible pre-release or build metadata cruft ("-alpha.0.1", "+abcde") from the end.
        var versionParts = version.Split('-', '+')[0].Split('.');

        if (versionParts is null || versionParts.Length != 3)
        {
            throw new Exception("Malformed app version: ${version}");
        }

        return versionParts.Select(part => Int32.Parse(part)).ToArray();
    }

    /// <summary>Extract version from release information</summary>
    /// <exception cref="ArgumentException">Throw if version number not found</exception>
    public static int[] ParseLatestVersion(string releaseJsonData)
    {
        var regex = new Regex(@"""tag_name"":""(\d+.\d+.\d+)""");
        MatchCollection matches = regex.Matches(releaseJsonData);

        if (matches.Count == 0)
        {
            throw new ArgumentException("Response didn't contain a valid release value");
        }

        return matches
            .Select(match => match.Groups[1].ToString().Split('.'))
            .Select(ver => ver.Select(part => Int32.Parse(part)).ToArray())
            .OrderByDescending(ver => ver, new SemVer())
            .First();
    }

    /// <summary>Read information about releases from GitHub</summary>
    /// <exception cref="HttpRequestException">Throw for non-success status code</exception>
    /// <exception cref="TaskCanceledException">Throw if request timeouts</exception>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
    public static async Task<string> FetchReleaseInformation()
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
        client.DefaultRequestHeaders.Add("User-Agent", Defaults.GITHUB_USER);

        var url = $"https://api.github.com/repos/{Defaults.GITHUB_USER}/{Defaults.GITHUB_REPO}/releases";
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }

    public static string GetSizeString(long byteSize)
    {
        double finalSize = byteSize;
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int suffixIndex = 0;
        while (finalSize >= 1024 && suffixIndex < suffixes.Length)
        {
            finalSize /= 1024;
            suffixIndex++;
        }
        return $"{finalSize:F2} {suffixes[suffixIndex]}";
    }
}
