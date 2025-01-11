// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CmdPal.Extensions;
using Microsoft.CmdPal.Extensions.Helpers;
using static EdgeFavoritesExtension.EdgeFavoritesApi;

namespace EdgeFavoritesExtension;

public partial class EdgeFavoritesExtensionActionsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;

    public EdgeFavoritesExtensionActionsProvider()
    {
        DisplayName = "Favorites (bookmarks) from Edge";

        var brandings = new[]
        {
            EdgeFavoritesApi.Branding.Stable,
            EdgeFavoritesApi.Branding.Beta,
            EdgeFavoritesApi.Branding.Canary,
            EdgeFavoritesApi.Branding.Dev,
        };

        Settings = new CommandSettings();

        _commands = brandings.Where(HasBranding).Select(b =>
        {
            return new CommandItem(new EdgeFavoritesExtensionPage(b))
            {
                Subtitle = $"Favorites (bookmarks) from {BrandingName(b)}",
                MoreCommands = [new CommandContextItem(Settings.SettingsPage)],
            };
        }).ToArray();
    }

    public override ICommandItem[] TopLevelCommands() => _commands;
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Sample code")]
public partial class SettingsPage : FormPage
{
    public SettingsPage()
    {
        Name = "Settings";
        Icon = new("\uE713"); // Settings
    }

    public override IForm[] Forms() => SettingsManager.Instance.Settings.ToForms();
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Sample code")]
public partial class CommandSettings : ICommandSettings
{
    private readonly SettingsPage _settingsPage = new();

    public IFormPage SettingsPage => _settingsPage;
}
