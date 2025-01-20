// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CmdPal.Extensions;
using Microsoft.CmdPal.Extensions.Helpers;
using WinGetExtension.Pages;

namespace WinGetExtension;

public partial class WinGetExtensionActionsProvider : CommandProvider
{
    public WinGetExtensionActionsProvider()
    {
        DisplayName = "WinGet for the Command Palette";
        _ = WinGetStatics.Manager;

        // _ = Task.Run(async () => await WinGetStatics.CompositeAllCatalog);
        // _ = Task.Run(async () => await WinGetStatics.CompositeWingetCatalog);
    }

    private readonly ICommandItem[] _commands = [
        new ListItem(new WinGetExtensionPage()),
        new ListItem(
            new WinGetExtensionPage("command-line") { Title = "tag:command-line" })
        {
            Title = "Search for command-line packages",
        },
        new ListItem(new InstalledPackagesPage())
    ];

    public override ICommandItem[] TopLevelCommands() => _commands;
}
