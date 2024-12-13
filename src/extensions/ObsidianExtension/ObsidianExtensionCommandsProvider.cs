// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CmdPal.Extensions;
using Microsoft.CmdPal.Extensions.Helpers;

namespace ObsidianExtension;

public partial class ObsidianExtensionActionsProvider : CommandProvider
{
    private readonly ObsidianExtensionPage _notesPage = new();
    private readonly ICommandItem[] _commands;

    public ObsidianExtensionActionsProvider()
    {
        DisplayName = "Obsidian Notes Commands";
        _commands = [
            new CommandItem(_notesPage),
        ];
    }

    public override ICommandItem[] TopLevelCommands() => _commands;
}
