﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace HackerNewsExtension;

public partial class HackerNewsCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _actions;

    public HackerNewsCommandsProvider()
    {
        DisplayName = "Hacker News Commands";
        Icon = HackerNewsPage.HackerNewsIcon;

        _actions = [
        new CommandItem(new HackerNewsPage())
        {
            Title = "Hacker News",
            Subtitle = "Search & Browse posts on Hacker News",
        },
        ];
    }

    public override ICommandItem[] TopLevelCommands() => _actions;
}
