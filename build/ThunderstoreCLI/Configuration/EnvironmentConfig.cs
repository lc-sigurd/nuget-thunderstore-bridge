/*
 * This file is largely based upon
 * https://github.com/thunderstore-io/thunderstore-cli/blob/10b73c843f2dd1a9ed9c6cb687dbbaa555626052/ThunderstoreCLI/Configuration/EnvironmentConfig.cs
 * thunderstore-cli Copyright (c) 2021 Thunderstore
 * Thunderstore expressly permits Lordfirespeed to use and redistribute the source of thunderstore-cli as Lordfirespeed sees fit.
 * Lordfirespeed licenses the referenced file to the Sigurd team under the MIT license.
 *
 * Copyright (c) 2024 Sigurd Team
 * The Sigurd Team licenses this file to you under the LGPL-3.0-OR-LATER license.
 */

using System;
using static System.Environment;

namespace ThunderstoreCLI.Configuration;

class EnvironmentConfig : EmptyConfig
{
    private const string AUTH_TOKEN = "THUNDERSTORE_API_TOKEN";

    public override AuthConfig GetAuthConfig()
    {
        return new AuthConfig
        {
            AuthToken = ReadEnv(AUTH_TOKEN)
        };
    }

    private string? ReadEnv(string variableName)
    {
        // Try to read the value from user-specific env variables.
        // This should result with up-to-date value on Windows, but
        // doesn't work on Linux/Mac.
        var value = GetEnvironmentVariable(AUTH_TOKEN, EnvironmentVariableTarget.User);


        // Alternatively try to read the value from process-specific
        // env variables. This works on Linux/Mac, but results in
        // outdated values if the env variable has been updated
        // after the shell was launched.
        if (String.IsNullOrWhiteSpace(value))
        {
            value = GetEnvironmentVariable(AUTH_TOKEN, EnvironmentVariableTarget.Process);
        }

        return value;
    }
}
