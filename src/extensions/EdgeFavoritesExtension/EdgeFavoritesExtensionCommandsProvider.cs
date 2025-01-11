// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CmdPal.Extensions;
using Microsoft.CmdPal.Extensions.Helpers;

namespace EdgeFavoritesExtension;

public partial class EdgeFavoritesExtensionActionsProvider : CommandProvider
{
    public EdgeFavoritesExtensionActionsProvider()
    {
        DisplayName = "Favorites (bookmarks) from Edge";
    }

    private readonly ICommandItem[] _commands = [
        new CommandItem(new EdgeFavoritesExtensionPage())
        {
            Title = "Edge Beta favorites",
            Subtitle = "Favorites (bookmarks) from Edge Beta",
        },

    ];

    public override ICommandItem[] TopLevelCommands() => _commands;
}
