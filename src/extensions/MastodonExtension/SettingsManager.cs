// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace MastodonExtension;

public class SettingsManager : JsonSettingsManager
{
    private static readonly Lazy<SettingsManager> Singleton = new(() => new SettingsManager());

    public static SettingsManager Instance => Singleton.Value;

    private readonly TextSetting _homeInstance = new(
        nameof(HomeInstance),
        "Home instance",
        "Your Mastodon hostname or HTTPS URL (for example, mastodon.social or https://fosstodon.org). Sign in separately on each instance.",
        MastodonInstance.DefaultUrl)
    {
        IsRequired = true,
        ErrorMessage = "A Mastodon instance is required.",
    };

    private readonly StatusMessage _validationStatus = new() { State = MessageState.Error };

    internal MastodonInstance HomeInstance { get; private set; } = MastodonInstance.Parse(MastodonInstance.DefaultUrl);

    public event EventHandler InstanceChanged;

    public SettingsManager()
        : this(SettingsJsonPath())
    {
    }

    internal SettingsManager(string filePath)
    {
        FilePath = filePath;
        Settings.Add(_homeInstance);
        LoadSettings();
        ApplyInstance();
        Settings.SettingsChanged += (s, e) => ApplyInstance();
    }

    private static string SettingsJsonPath()
    {
        var directory = Utilities.BaseSettingsPath("com.zadjii.mastodon-extension");
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, "settings.json");
    }

    private void ApplyInstance()
    {
        MastodonInstance instance;
        try
        {
            instance = MastodonInstance.Parse(_homeInstance.Value);
        }
        catch (ArgumentException e)
        {
            _homeInstance.Value = HomeInstance.Url;
            SaveSettings();
            ExtensionHost.LogMessage(new LogMessage() { Message = e.Message });
            _validationStatus.Message = e.Message;
            ExtensionHost.ShowStatus(_validationStatus, StatusContext.Extension);
            return;
        }

        ExtensionHost.HideStatus(_validationStatus);
        var changed = instance.Url != HomeInstance.Url;
        HomeInstance = instance;
        _homeInstance.Value = instance.Url;
        SaveSettings();
        if (changed)
        {
            InstanceChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
