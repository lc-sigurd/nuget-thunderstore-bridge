/*
 * This file is largely based upon
 * https://github.com/thunderstore-io/thunderstore-cli/blob/10b73c843f2dd1a9ed9c6cb687dbbaa555626052/ThunderstoreCLI/Configuration/EmptyConfig.cs
 * thunderstore-cli Copyright (c) 2021 Thunderstore
 * Thunderstore expressly permits Lordfirespeed to use and redistribute the source of thunderstore-cli as Lordfirespeed sees fit.
 * Lordfirespeed licenses the referenced file to the Sigurd team under the MIT license.
 *
 * Copyright (c) 2024 Sigurd Team
 * The Sigurd Team licenses this file to you under the LGPL-3.0-OR-LATER license.
 */

namespace ThunderstoreCLI.Configuration;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
public abstract class EmptyConfig : IConfigProvider
{
    public virtual void Parse(Config currentConfig) { }
    public virtual GeneralConfig? GetGeneralConfig() => null;

    public virtual PackageConfig? GetPackageMeta() => null;

    public virtual InitConfig? GetInitConfig() => null;

    public virtual BuildConfig? GetBuildConfig() => null;

    public virtual PublishConfig? GetPublishConfig() => null;

    public virtual InstallConfig? GetInstallConfig() => null;

    public virtual AuthConfig? GetAuthConfig() => null;

    public virtual ModManagementConfig? GetModManagementConfig() => null;

    public virtual GameImportConfig? GetGameImportConfig() => null;

    public virtual RunGameConfig? GetRunGameConfig() => null;
}
