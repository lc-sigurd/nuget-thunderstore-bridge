/*
 * This file is largely based upon
 * https://github.com/thunderstore-io/thunderstore-cli/blob/10b73c843f2dd1a9ed9c6cb687dbbaa555626052/ThunderstoreCLI/Utils/StringUtils.cs
 * thunderstore-cli Copyright (c) 2021 Thunderstore
 * Thunderstore expressly permits Lordfirespeed to use and redistribute the source of thunderstore-cli as Lordfirespeed sees fit.
 * Lordfirespeed licenses the referenced file to the Sigurd team under the MIT license.
 *
 * Copyright (c) 2024 Sigurd Team
 * The Sigurd Team licenses this file to you under the LGPL-3.0-OR-LATER license.
 */

using System.Text.RegularExpressions;

namespace ThunderstoreCLI.Utils;

public static class StringUtils
{
    private static readonly Regex SemVerRegex = new(@"^[0-9]+\.[0-9]+\.[0-9]+$");

    /// <summary>
    /// Validate the given string adheres to MAJOR.MINOR.PATCH format
    /// </summary>
    /// <remarks>
    /// Prerelease and build postfixes are not supported and will
    /// return false.
    /// </remarks>
    public static bool IsSemVer(string version)
    {
        return SemVerRegex.IsMatch(version);
    }
}
