/*
 * Copyright (c) 2024 Sigurd Team
 * The Sigurd Team licenses this file to you under the GPL-3.0-OR-LATER license.
 */

using System.Text.Json.Serialization;
using Json.Schema;
using Json.Schema.Serialization;

namespace Build.Schema;

[JsonSchema(typeof(CommunityConfiguration), nameof(CommunityConfigurationSchema))]
public class CommunityConfiguration
{
	public static JsonSchema CommunityConfigurationSchema = JsonSchema.FromFile("../assets/community.schema.json");

    [JsonPropertyName("communitySlug")]
    public string CommunitySlug { get; set; }

    [JsonPropertyName("runtimeFrameworkMoniker")]
    public string RuntimeFrameworkMoniker { get; set; }

    [JsonPropertyName("packageNamespace")]
    public string PackageNamespace { get; set; }
}
