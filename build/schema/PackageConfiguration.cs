/*
 * Copyright (c) 2024 Sigurd Team
 * The Sigurd Team licenses this file to you under the GPL-3.0-OR-LATER license.
 */

using System.Text.Json.Serialization;
using Json.Schema;
using Json.Schema.Serialization;

namespace Build.Schema;

[JsonSchema(typeof(PackageConfiguration), nameof(PackageConfigurationSchema))]
public class PackageConfiguration
{
	public static JsonSchema PackageConfigurationSchema = JsonSchema.FromFile("../assets/package.schema.json");

	[JsonPropertyName("packageId")]
    public string PackageId { get; set; }
}
