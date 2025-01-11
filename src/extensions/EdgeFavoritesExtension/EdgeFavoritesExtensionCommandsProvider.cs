// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
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
        _commands = brandings.Where(HasBranding).Select(b =>
        {
            return new CommandItem(new EdgeFavoritesExtensionPage(b))
            {
                Subtitle = $"Favorites (bookmarks) from {BrandingName(b)}",
            };
        }).ToArray();
    }

    public override ICommandItem[] TopLevelCommands() => _commands;
}
