// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace EdgeFavoritesExtension;

public class SettingsManager : JsonSettingsManager
{
    public static SettingsManager Instance { get; } = new();

    private readonly ToggleSetting _flatList = new(
        nameof(FlatList),
        "Display items in flat list",
        "When enabled, all bookmarks will be shown in a singular list (without nesting)",
        false);

    public bool FlatList => _flatList.Value;

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

        Settings.Add(_flatList);

        // Load settings from file upon initialization
        LoadSettings();

        Settings.SettingsChanged += (s, e) => SaveSettings();
    }
}
