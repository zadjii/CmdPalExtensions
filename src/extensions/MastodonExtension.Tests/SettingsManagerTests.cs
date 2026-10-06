// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text.Json;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MastodonExtension;

[TestClass]
public class SettingsManagerTests
{
    private string _directory = string.Empty;
    private string _filePath = string.Empty;

    [TestInitialize]
    public void Initialize()
    {
        _directory = Path.Combine(AppContext.BaseDirectory, $"instance-settings-{Guid.NewGuid()}");
        Directory.CreateDirectory(_directory);
        _filePath = Path.Combine(_directory, "settings.json");
    }

    [TestCleanup]
    public void Cleanup()
    {
        File.Delete(_filePath);
        Directory.Delete(_directory);
    }

    [TestMethod]
    public void DefaultsToMastodonSocial()
    {
        var manager = new SettingsManager(_filePath);

        Assert.AreEqual(MastodonInstance.DefaultUrl, manager.HomeInstance.Url);
    }

    [TestMethod]
    public void PersistsNormalizedInstanceAndRaisesChangeEvent()
    {
        var manager = new SettingsManager(_filePath);
        var changes = 0;
        manager.InstanceChanged += (s, e) => changes++;

        UpdateInstance(manager, " https://FOSSTODON.ORG/ ");
        var reloaded = new SettingsManager(_filePath);

        Assert.AreEqual("https://fosstodon.org", manager.HomeInstance.Url);
        Assert.AreEqual(manager.HomeInstance.Url, reloaded.HomeInstance.Url);
        Assert.AreEqual(1, changes);
    }

    [TestMethod]
    public void EquivalentInstanceDoesNotResetLogin()
    {
        var manager = new SettingsManager(_filePath);
        var changes = 0;
        manager.InstanceChanged += (s, e) => changes++;

        UpdateInstance(manager, "https://MASTODON.SOCIAL:443/");

        Assert.AreEqual(MastodonInstance.DefaultUrl, manager.HomeInstance.Url);
        Assert.AreEqual(0, changes);
    }

    [TestMethod]
    [DataRow("http://social.example")]
    [DataRow("https://social.example/@user")]
    [DataRow("")]
    public void InvalidInstancePreservesPreviousServerAndSavedSetting(string input)
    {
        var manager = new SettingsManager(_filePath);
        UpdateInstance(manager, "fosstodon.org");
        var changes = 0;
        manager.InstanceChanged += (s, e) => changes++;

        UpdateInstance(manager, input);
        var reloaded = new SettingsManager(_filePath);

        Assert.AreEqual("https://fosstodon.org", manager.HomeInstance.Url);
        Assert.AreEqual(manager.HomeInstance.Url, reloaded.HomeInstance.Url);
        Assert.AreEqual(0, changes);
    }

    private static void UpdateInstance(SettingsManager manager, string value)
    {
        var form = (SettingsForm)manager.Settings.ToContent()[0];
        form.SubmitForm(JsonSerializer.Serialize(new { HomeInstance = value }), "{}");
    }
}
