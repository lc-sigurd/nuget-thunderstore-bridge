/*
 * This file is largely based upon
 * https://github.com/thunderstore-io/thunderstore-cli/blob/10b73c843f2dd1a9ed9c6cb687dbbaa555626052/ThunderstoreCLI/Utils/Comparers.cs
 * thunderstore-cli Copyright (c) 2021 Thunderstore
 * Thunderstore expressly permits Lordfirespeed to use and redistribute the source of thunderstore-cli as Lordfirespeed sees fit.
 * Lordfirespeed licenses the referenced file to the Sigurd team under the MIT license.
 *
 * Copyright (c) 2024 Sigurd Team
 * The Sigurd Team licenses this file to you under the LGPL-3.0-OR-LATER license.
 */

using System.Collections.Generic;

namespace ThunderstoreCLI.Utils;

public class SemVer : IComparer<int[]>
{
    /// <summary>Compare two int arrays containing SemVer parts</summary>
    /// <remarks>Each parameter should have length of 3</remarks>
    public int Compare(int[]? a, int[]? b)
    {
        // IComparer forces us to accept nullable parameters, even
        // though in this case they make no sense.
        if (a is null && b is null)
        {
            return 0;
        }
        else if (a is null)
        {
            return -1;
        }
        else if (b is null)
        {
            return 1;
        }

        if (a[0] != b[0])
        {
            return a[0].CompareTo(b[0]);
        }

        if (a[1] != b[1])
        {
            return a[1].CompareTo(b[1]);
        }

        return a[2].CompareTo(b[2]);
    }
}
