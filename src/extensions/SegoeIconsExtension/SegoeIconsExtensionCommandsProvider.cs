// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CmdPal.Extensions;
using Microsoft.CmdPal.Extensions.Helpers;

namespace SegoeIconsExtension;

public partial class SegoeIconsExtensionActionsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;

    public SegoeIconsExtensionActionsProvider()
    {
        DisplayName = "Segoe Icons Commands";
        var page = new SegoeIconsExtensionPage();
        Icon = page.Icon;

        _commands = [
            new CommandItem(page)
            {
                Title = "Segoe Icons",
                Subtitle = "Search Segoe Fluent Icons",
                MoreCommands = [
                    new CommandContextItem(new OpenUrlCommand("https://learn.microsoft.com/en-us/windows/apps/design/style/segoe-fluent-icons-font"))
                    {
                        Title = "Open on web",
                    },
                    new CommandContextItem(new OpenUrlCommand("winui3gallery://item/Iconography"))
                    {
                        Title = "Open WinUI 3 gallery",
                        Icon = page.Icon,
                    }
                ],
            },
        ];
    }

    public override ICommandItem[] TopLevelCommands() => _commands;
}
