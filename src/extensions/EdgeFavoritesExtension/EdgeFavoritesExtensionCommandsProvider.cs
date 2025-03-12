// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using static EdgeFavoritesExtension.EdgeFavoritesApi;

namespace EdgeFavoritesExtension;

public partial class EdgeFavoritesExtensionActionsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;

    public EdgeFavoritesExtensionActionsProvider()
    {
        DisplayName = "Favorites (bookmarks) from Edge";
        Settings = SettingsManager.Instance.Settings;
        var brandings = new[]
        {
            EdgeFavoritesApi.Branding.Stable,
            EdgeFavoritesApi.Branding.Beta,
            EdgeFavoritesApi.Branding.Canary,
            EdgeFavoritesApi.Branding.Dev,
        };

        // Settings = new CommandSettings();
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
