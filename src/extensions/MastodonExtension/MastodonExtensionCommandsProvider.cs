// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace MastodonExtension;

public partial class MastodonExtensionCommandsProvider : CommandProvider
{
    public static ApiConfig Config { get; } = new();

    private readonly CommandItem _loginItem;
    private readonly CommandItem _exploreItem;
    private readonly CommandItem _homeItem;
    private readonly CommandItem _logoutItem;

    public MastodonExtensionCommandsProvider()
    {
        DisplayName = "Mastodon for CmdPal";
        Icon = MastodonExtensionPage.MastodonIcon;

        _loginItem = new CommandItem(new MastodonLoginPage());
        _exploreItem = new CommandItem(new MastodonExtensionPage(isExplorePage: true))
        {
            Title = "Explore Mastodon",
            Subtitle = "Explore top posts on mastodon.social",
        };
        _homeItem = new CommandItem(new MastodonExtensionPage(isExplorePage: false))
        {
            Title = "Mastodon",
            Subtitle = "Posts from users and tags you follow on Mastodon",
        };
        _logoutItem = new CommandItem(new LogoutCommand())
        {
            Subtitle = "Log out of Mastodon",
        };

        ApiConfig.UserLoginChanged += (s, e) => RaiseItemsChanged(1);
    }

    public override ICommandItem[] TopLevelCommands()
    {
        if (!ApiConfig.HasAppId)
        {
            ApiConfig.GetClientIdAndSecret().ConfigureAwait(false);
        }

        if (ApiConfig.HasUserToken)
        {
            return [_homeItem, _exploreItem, _logoutItem];
        }
        else
        {
            ExtensionHost.LogMessage(new LogMessage() { Message = "User was not logged in" });
            return [_loginItem, _exploreItem];
        }
    }
}
