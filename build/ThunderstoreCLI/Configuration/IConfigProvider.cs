/*
 * This file is largely based upon
 * https://github.com/thunderstore-io/thunderstore-cli/blob/10b73c843f2dd1a9ed9c6cb687dbbaa555626052/ThunderstoreCLI/Configuration/IConfigProvider.cs
 * thunderstore-cli Copyright (c) 2021 Thunderstore
 * Thunderstore expressly permits Lordfirespeed to use and redistribute the source of thunderstore-cli as Lordfirespeed sees fit.
 * Lordfirespeed licenses the referenced file to the Sigurd team under the MIT license.
 *
 * Copyright (c) 2024 Sigurd Team
 * The Sigurd Team licenses this file to you under the LGPL-3.0-OR-LATER license.
 */

namespace ThunderstoreCLI.Configuration;

public interface IConfigProvider
{
    void Parse(Config currentConfig);

    GeneralConfig? GetGeneralConfig();
    PackageConfig? GetPackageMeta();
    InitConfig? GetInitConfig();
    BuildConfig? GetBuildConfig();
    PublishConfig? GetPublishConfig();
    InstallConfig? GetInstallConfig();
    AuthConfig? GetAuthConfig();
    ModManagementConfig? GetModManagementConfig();
    GameImportConfig? GetGameImportConfig();
    RunGameConfig? GetRunGameConfig();
}
