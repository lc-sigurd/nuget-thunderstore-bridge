/*
 * This file is largely based upon
 * https://github.com/thunderstore-io/thunderstore-cli/blob/10b73c843f2dd1a9ed9c6cb687dbbaa555626052/ThunderstoreCLI/Configuration/DefaultConfig.cs
 * thunderstore-cli Copyright (c) 2021 Thunderstore
 * Thunderstore expressly permits Lordfirespeed to use and redistribute the source of thunderstore-cli as Lordfirespeed sees fit.
 * Lordfirespeed licenses the referenced file to the Sigurd team under the MIT license.
 *
 * Copyright (c) 2024 Sigurd Team
 * The Sigurd Team licenses this file to you under the LGPL-3.0-OR-LATER license.
 */

using System.Collections.Generic;
using ThunderstoreCLI.Utils;

namespace ThunderstoreCLI.Configuration;

class DefaultConfig : EmptyConfig
{
    public override GeneralConfig GetGeneralConfig()
    {
        return new GeneralConfig
        {
            Repository = Defaults.REPOSITORY_URL
        };
    }

    public override PackageConfig GetPackageMeta()
    {
        return new PackageConfig
        {
            ProjectPath = Defaults.PROJECT_CONFIG_PATH,
            Namespace = "AuthorName",
            Name = "PackageName",
            VersionNumber = "0.0.1",
            Description = "Example mod description",
            WebsiteUrl = "https://thunderstore.io",
            ContainsNsfwContent = false,
            Dependencies = new()
            {
                { "AuthorName-PackageName", "0.0.1" }
            }
        };
    }

    public override InitConfig GetInitConfig()
    {
        return new InitConfig
        {
            Overwrite = false
        };
    }

    public override BuildConfig GetBuildConfig()
    {
        return new BuildConfig
        {
            IconPath = "./icon.png",
            ReadmePath = "./README.md",
            OutDir = "./build",
            CopyPaths = [new("./dist", "")]
        };
    }

    public override PublishConfig GetPublishConfig()
    {
        return new PublishConfig
        {
            File = null,
            Communities = ["riskofrain2"],
            Categories = new Dictionary<string, string[]> {
                { "riskofrain2", ["items", "skills", ] },
            }
        };
    }

    public override InstallConfig GetInstallConfig()
    {
        return new InstallConfig
        {
            InstallerDeclarations = [new InstallerDeclaration("foo-installer")]
        };
    }
}
