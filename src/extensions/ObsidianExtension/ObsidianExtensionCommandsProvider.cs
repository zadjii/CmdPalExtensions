// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text.Json.Nodes;
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
public class SettingsManager
{
    public static SettingsManager Instance { get; } = new();

    private readonly string _filePath;
    private readonly Settings _settings = new();

    private readonly TextSetting _vaultPath = new(
        nameof(VaultPath),
        "Path to your Obsidian vault",
        "Input the path to the folder where your Obsidian vault is stored",
        string.Empty);

    public string VaultPath => _vaultPath.Value;

    internal static string SettingsJsonPath()
    {
        // Get the path to our exe
        var path = System.Reflection.Assembly.GetExecutingAssembly().Location;

        // Get the directory of the exe
        var directory = Path.GetDirectoryName(path) ?? string.Empty;

        // now, the state is just next to the exe
        return Path.Combine(directory, "settings.json");
    }

    public SettingsManager()
    {
        _filePath = SettingsJsonPath();

        _settings.Add(_vaultPath);

        // Load settings from file upon initialization
        LoadSettings();

        _settings.SettingsChanged += (s, e) => SaveSettings();
    }

    public Settings GetSettings() => _settings;

    public void SaveSettings()
    {
        try
        {
            // Serialize the main dictionary to JSON and save it to the file
            var settingsJson = _settings.ToJson();

            File.WriteAllText(_filePath, settingsJson);
        }
        catch (Exception ex)
        {
            ExtensionHost.LogMessage(new LogMessage() { Message = ex.ToString() });
        }
    }

    public void LoadSettings()
    {
        if (!File.Exists(_filePath))
        {
            ExtensionHost.LogMessage(new LogMessage() { Message = "The provided settings file does not exist" });
            return;
        }

        try
        {
            // Read the JSON content from the file
            var jsonContent = File.ReadAllText(_filePath);

            // Is it valid JSON?
            if (JsonNode.Parse(jsonContent) is JsonObject savedSettings)
            {
                _settings.Update(jsonContent);
            }
            else
            {
                ExtensionHost.LogMessage(new LogMessage() { Message = "Failed to parse settings file as JsonObject." });
            }
        }
        catch (Exception ex)
        {
            ExtensionHost.LogMessage(new LogMessage() { Message = ex.ToString() });
        }
    }
}
