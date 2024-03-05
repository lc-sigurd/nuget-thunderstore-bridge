/*
 * This file is largely based upon
 * https://github.com/thunderstore-io/thunderstore-cli/blob/10b73c843f2dd1a9ed9c6cb687dbbaa555626052/ThunderstoreCLI/Utils/DictionaryExtensions.cs
 * thunderstore-cli Copyright (c) 2021 Thunderstore
 * Thunderstore expressly permits Lordfirespeed to use and redistribute the source of thunderstore-cli as Lordfirespeed sees fit.
 * Lordfirespeed licenses the referenced file to the Sigurd team under the MIT license.
 *
 * Copyright (c) 2024 Sigurd Team
 * The Sigurd Team licenses this file to you under the LGPL-3.0-OR-LATER license.
 */

using System.Collections.Generic;

namespace ThunderstoreCLI.Utils;

public static class DictionaryExtensions
{
    public static TValue? GetOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key) where TKey : notnull
    {
        return dict.TryGetValue(key, out var value) ? value : default;
    }
}
