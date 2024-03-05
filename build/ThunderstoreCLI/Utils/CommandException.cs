/*
 * Copyright (c) 2024 Sigurd Team
 * The Sigurd Team licenses this file to you under the LGPL-3.0-OR-LATER license.
 */

using System;

namespace ThunderstoreCLI.Utils;

internal class CommandException : Exception
{
    public CommandException() : base() { }
    public CommandException(string message) : base(message) { }
    public CommandException(string message, Exception reason) : base(message, reason) { }
}
