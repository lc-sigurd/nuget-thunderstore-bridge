/*
 * This file is largely based upon
 * https://github.com/thunderstore-io/thunderstore-cli/blob/10b73c843f2dd1a9ed9c6cb687dbbaa555626052/ThunderstoreCLI/Models/BaseJson.cs
 * thunderstore-cli Copyright (c) 2021 Thunderstore
 * Thunderstore expressly permits Lordfirespeed to use and redistribute the source of thunderstore-cli as Lordfirespeed sees fit.
 * Lordfirespeed licenses the referenced file to the Sigurd team under the MIT license.
 *
 * Copyright (c) 2024 Sigurd Team
 * The Sigurd Team licenses this file to you under the LGPL-3.0-OR-LATER license.
 */

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ThunderstoreCLI.Models;

public abstract class BaseJson<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T> : ISerialize<T>
    where T : BaseJson<T>
{
    public string Serialize() => Serialize(null);
    public string Serialize(JsonSerializerSettings? options)
    {
        return JsonConvert.SerializeObject(this, options);
    }

    public static T? Deserialize(string json) => Deserialize(json, null);
    public static T? Deserialize(Stream stream)
    {
        using var reader = new StreamReader(stream);
        return JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
    }
    public static T? Deserialize(string json, JsonSerializerSettings? options)
    {
        return JsonConvert.DeserializeObject<T>(json, options);
    }

    public static ValueTask<T?> DeserializeAsync(string json) => new(Deserialize(json));
    public static ValueTask<T?> DeserializeAsync(Stream json) => new(DeserializeAsync(json, null));
    public static async Task<T?> DeserializeAsync(Stream json, JsonSerializerSettings? options)
    {
        using StreamReader reader = new(json);
        return Deserialize(await reader.ReadToEndAsync(), options);
    }

    public static List<T>? DeserializeList(string json, JsonSerializerSettings? options = null)
    {
        return JsonConvert.DeserializeObject<List<T>>(json, options);
    }
}

public static class BaseJson
{
    public static readonly JsonSerializerSettings IndentedSettings = new()
    {
        Formatting = Formatting.Indented
    };
}

public static class BaseJsonExtensions
{
    public static string SerializeList<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(this List<T> list, JsonSerializerSettings? options = null)
        where T : BaseJson<T>
    {
        return JsonConvert.SerializeObject(list, options);
    }
}
