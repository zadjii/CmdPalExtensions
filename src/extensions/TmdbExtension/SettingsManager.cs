// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace TmdbExtension;

public class SettingsManager : JsonSettingsManager
{
    private static readonly Lazy<SettingsManager> _instance = new(() => new SettingsManager());

    public static SettingsManager Instance => _instance.Value;

    private readonly TextSetting _language = new(
        nameof(Language),
        "Language",
        "Language code for movie results and details, such as en-US, fr, or it. Leave blank to use en-US.",
        "en-US");

    public string Language => string.IsNullOrWhiteSpace(_language.Value) ? "en-US" : _language.Value.Trim();

    public SettingsManager()
        : this(SettingsJsonPath())
    {
    }

    internal SettingsManager(string filePath)
    {
        FilePath = filePath;
        Settings.Add(_language);
        LoadSettings();
        Settings.SettingsChanged += (s, e) => SaveSettings();
    }

    private static string SettingsJsonPath()
    {
        var directory = Utilities.BaseSettingsPath("com.zadjii.tmdb-extension");
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, "settings.json");
    }
}
