/*
 * This file is largely based upon
 * https://github.com/thunderstore-io/thunderstore-cli/blob/10b73c843f2dd1a9ed9c6cb687dbbaa555626052/ThunderstoreCLI/Commands/CommandValidator.cs
 * thunderstore-cli Copyright (c) 2021 Thunderstore
 * Thunderstore expressly permits Lordfirespeed to use and redistribute the source of thunderstore-cli as Lordfirespeed sees fit.
 * Lordfirespeed licenses the referenced file to the Sigurd team under the MIT license.
 *
 * Copyright (c) 2024 Sigurd Team
 * The Sigurd Team licenses this file to you under the LGPL-3.0-OR-LATER license.
 */

using System.Collections.Generic;
using ThunderstoreCLI.Utils;

namespace ThunderstoreCLI.Commands;

/// <summary>Helper for validating command-specific configurations</summary>
public class CommandValidator
{
    private List<string> _errors;
    private string _name;

    public CommandValidator(string commandName, List<string>? errors = null)
    {
        _name = commandName;
        _errors = errors ?? new List<string>();
    }

    /// <summary>Add given errorMessage if isError is true</summary>
    /// <returns>Value of passed isError</returns>
    public bool Add(bool isError, string errorMessage)
    {
        if (isError)
        {
            _errors.Add(errorMessage);
        }
        return isError;
    }

    /// <summary> Add error if given value is null or empty-ish string</summary>
    /// <returns>True if value is empty</returns>
    public bool AddIfEmpty(string? value, string settingName)
    {
        return Add(
            string.IsNullOrWhiteSpace(value),
            $"{settingName} setting can't be empty"
        );
    }
    public bool AddIfNotSemver(string? version, string settingName)
    {
        if (AddIfEmpty(version, settingName))
        {
            return true;
        }

        return Add(
            !StringUtils.IsSemVer(version!),
            $"Invalid package version number \"{version}\". Version numbers must follow the Major.Minor.Patch format (e.g. 1.45.320)"
        );
    }


    /// <summary>Add error if given value is null</summary>
    /// <returns>True if value is null</returns>
    public bool AddIfNull<T>(T value, string settingName)
    {
        return Add(
            value is null,
            $"{settingName} setting can't be null"
        );
    }

    public List<string> GetErrors() => _errors;

    /// <summary>Output any added error messages to Console</summary>
    /// <exception cref="CommandException">Throw if any errors were added</exception>
    public void ThrowIfErrors()
    {
        if (_errors.Count > 0)
        {
            Write.ErrorExit($"Invalid configuration to run '{_name}' command", _errors.ToArray());
            throw new CommandException("Invalid config for InitCommand");
        }
    }
}
