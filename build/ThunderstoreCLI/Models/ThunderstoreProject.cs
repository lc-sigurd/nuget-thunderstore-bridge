/*
 * This file is largely based upon
 * https://github.com/thunderstore-io/thunderstore-cli/blob/10b73c843f2dd1a9ed9c6cb687dbbaa555626052/ThunderstoreCLI/Models/ThunderstoreProject.cs
 * thunderstore-cli Copyright (c) 2021 Thunderstore
 * Thunderstore expressly permits Lordfirespeed to use and redistribute the source of thunderstore-cli as Lordfirespeed sees fit.
 * Lordfirespeed licenses the referenced file to the Sigurd team under the MIT license.
 *
 * Copyright (c) 2024 Sigurd Team
 * The Sigurd Team licenses this file to you under the LGPL-3.0-OR-LATER license.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using ThunderstoreCLI.Configuration;
using Tomlet;
using Tomlet.Attributes;
using Tomlet.Models;

namespace ThunderstoreCLI.Models;

[TomlDoNotInlineObject]
public class ThunderstoreProject : BaseToml<ThunderstoreProject>
{
    public struct CategoryDictionary
    {
        public Dictionary<string, string[]> Categories;
    }

    static ThunderstoreProject()
    {
        TomletMain.RegisterMapper(
            dict => TomletMain.ValueFrom(dict.Categories),
            toml => toml switch
            {
                TomlArray arr => new CategoryDictionary
                {
                    Categories = new Dictionary<string, string[]>
                    {
                        { "", arr.ArrayValues.Select(v => v.StringValue).ToArray() }
                    }
                },
                TomlTable table => new CategoryDictionary { Categories = TomletMain.To<Dictionary<string, string[]>>(table) },
                _ => throw new NotSupportedException()
            });
    }

    [TomlDoNotInlineObject]
    public class ConfigData
    {
        [TomlProperty("schemaVersion")]
        public string SchemaVersion { get; set; } = "0.0.1";
    }

    [TomlProperty("config")]
    public ConfigData? Config { get; set; } = new();

    [TomlDoNotInlineObject]
    public class PackageData
    {
        [TomlProperty("namespace")]
        public string? Namespace { get; set; }
        [TomlProperty("name")]
        public string? Name { get; set; }
        [TomlProperty("versionNumber")]
        public string? VersionNumber { get; set; }
        [TomlProperty("description")]
        public string? Description { get; set; }
        [TomlProperty("websiteUrl")]
        public string? WebsiteUrl { get; set; }
        [TomlProperty("containsNsfwContent")]
        public bool ContainsNsfwContent { get; set; } = false;

        [TomlProperty("dependencies")]
        [TomlDoNotInlineObject]
        public Dictionary<string, string> Dependencies { get; set; } = new();
    }
    [TomlProperty("package")]
    public PackageData? Package { get; set; }

    [TomlDoNotInlineObject]
    public class BuildData
    {
        [TomlProperty("icon")]
        public string? Icon { get; set; }
        [TomlProperty("readme")]
        public string? Readme { get; set; }
        [TomlProperty("outdir")]
        public string? OutDir { get; set; }

        [TomlDoNotInlineObject]
        public class CopyPath
        {
            [TomlProperty("source")]
            public string? Source { get; set; }
            [TomlProperty("target")]
            public string? Target { get; set; }
        }

        [TomlProperty("copy")]
        public CopyPath[] CopyPaths { get; set; } = Array.Empty<CopyPath>();
    }
    [TomlProperty("build")]
    public BuildData? Build { get; set; }

    [TomlDoNotInlineObject]
    public class PublishData
    {
        [TomlProperty("repository")]
        public string? Repository { get; set; }

        [TomlProperty("communities")]
        public string[] Communities { get; set; } = Array.Empty<string>();

        [TomlProperty("categories")]
        [TomlDoNotInlineObject]
        public CategoryDictionary Categories { get; set; } = new()
        {
            Categories = new Dictionary<string, string[]>(),
        };
    }
    [TomlProperty("publish")]
    public PublishData? Publish { get; set; }

    [TomlDoNotInlineObject]
    public class InstallData
    {
        [TomlDoNotInlineObject]
        public class InstallerDeclaration
        {
            [TomlProperty("identifier")]
            public string? Identifier { get; set; }
        }

        [TomlProperty("installers")]
        public InstallerDeclaration[] InstallerDeclarations { get; set; } = Array.Empty<InstallerDeclaration>();
    }
    [TomlProperty("install")]
    public InstallData? Install { get; set; }

    public ThunderstoreProject() { }

    public ThunderstoreProject(bool initialize)
    {
        if (!initialize)
            return;

        Package = new PackageData();
        Build = new BuildData();
        Publish = new PublishData();
        Install = new InstallData();
    }

    public ThunderstoreProject(Config config)
    {
        Package = new PackageData
        {
            Namespace = config.PackageConfig.Namespace!,
            Name = config.PackageConfig.Name!,
            VersionNumber = config.PackageConfig.VersionNumber!,
            Description = config.PackageConfig.Description!,
            WebsiteUrl = config.PackageConfig.WebsiteUrl!,
            ContainsNsfwContent = config.PackageConfig.ContainsNsfwContent.GetValueOrDefault(false),
            Dependencies = config.PackageConfig.Dependencies!
        };
        Build = new BuildData
        {
            Icon = config.BuildConfig.IconPath!,
            OutDir = config.BuildConfig.OutDir!,
            Readme = config.BuildConfig.ReadmePath!,
            CopyPaths = config.BuildConfig.CopyPaths!
                .Select(x => new BuildData.CopyPath { Source = x.From, Target = x.To })
                .ToArray()
        };
        Publish = new PublishData
        {
            Categories = new CategoryDictionary { Categories = config.PublishConfig.Categories! },
            Communities = config.PublishConfig.Communities!,
            Repository = config.GeneralConfig.Repository
        };
        Install = new InstallData
        {
            InstallerDeclarations = config.InstallConfig.InstallerDeclarations!
                .Select(x => new InstallData.InstallerDeclaration { Identifier = x.Identifier })
                .ToArray()
        };
    }
}
