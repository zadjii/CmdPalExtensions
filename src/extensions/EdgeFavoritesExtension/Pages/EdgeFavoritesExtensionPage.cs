// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CmdPal.Extensions;
using Microsoft.CmdPal.Extensions.Helpers;

namespace EdgeFavoritesExtension;

internal sealed partial class EdgeFavoritesExtensionPage : ListPage
{
    public EdgeFavoritesExtensionPage()
    {
        Icon = new(string.Empty);
        Name = "Favorites (bookmarks) from Edge";
    }

    public override IListItem[] GetItems()
    {
        return [
            new ListItem(new NoOpCommand()) { Title = "TODO: Implement your extension here" }
        ];
    }
}
