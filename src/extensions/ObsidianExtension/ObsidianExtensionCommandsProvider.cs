// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

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
            new CommandItem(_notesPage)
            {
                MoreCommands = [new CommandContextItem(new SettingsPage())],
            },
            new CommandItem(_newNoteCommand) { Title = "New note", Subtitle = "Open a new note in Obsidian" },
            new CommandItem(_dailyNote) { Title = "Open daily note", Subtitle = "Open your daily note in Obsidian" },
        ];
    }

    public override ICommandItem[] TopLevelCommands() => _commands;
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Sample code")]
public class SettingsManager : JsonSettingsManager
{
    public static SettingsManager Instance { get; } = new();

    private readonly TextSetting _vaultPath = new(
        nameof(VaultPath),
        "Path to your Obsidian vault",
        "Input the path to the folder where your Obsidian vault is stored",
        string.Empty);

    public string VaultPath => _vaultPath.Value;

    internal static string SettingsJsonPath()
    {
        var directory = Utilities.BaseSettingsPath("com.zadjii.obsidian-extension");
        Directory.CreateDirectory(directory);

        // now, the state is just next to the exe
        return Path.Combine(directory, "settings.json");
    }

    public SettingsManager()
    {
        FilePath = SettingsJsonPath();

        Settings.Add(_vaultPath);

        // Load settings from file upon initialization
        LoadSettings();

        Settings.SettingsChanged += (s, e) => SaveSettings();
    }
}
