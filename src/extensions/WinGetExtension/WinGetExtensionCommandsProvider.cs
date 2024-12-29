// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CmdPal.Extensions;
using Microsoft.CmdPal.Extensions.Helpers;

namespace WinGetExtension;

public partial class WinGetExtensionActionsProvider : CommandProvider
{
    public WinGetExtensionActionsProvider()
    {
        DisplayName = "WinGet for the Command Palette";
    }

    private readonly IListItem[] _commands = [
        new ListItem(new WinGetExtensionPage()),
        new ListItem(new InstalledPackagesPage())
    ];

    public override IListItem[] TopLevelCommands() => _commands;
}
