// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CmdPal.Extensions;
using Microsoft.CmdPal.Extensions.Helpers;

namespace ObsidianExtension;

public partial class ObsidianExtensionActionsProvider : CommandProvider
{
    private readonly ObsidianExtensionPage _notesPage = new();
    private readonly NewNoteCommand _newNoteCommand = new();
    private readonly OpenDailyNoteCommand _dailyNote = new();
    private readonly ICommandItem[] _commands;

    public ObsidianExtensionActionsProvider()
    {
        DisplayName = "Obsidian Notes Commands";
        _commands = [
            new CommandItem(_notesPage),
            new CommandItem(_newNoteCommand) { Title = "New note", Subtitle = "Open a new note in Obsidian" },
            new CommandItem(_dailyNote) { Title = "Open daily note", Subtitle = "Open your daily note in Obsidian" },
        ];
    }

    public override ICommandItem[] TopLevelCommands() => _commands;
}
