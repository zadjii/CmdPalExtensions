// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CmdPal.Extensions;
using Microsoft.CmdPal.Extensions.Helpers;

namespace EdgeFavoritesExtension;

internal sealed partial class EdgeFavoritesExtensionPage : ListPage
{
    private IListItem[]? _items;

    public EdgeFavoritesExtensionPage()
    {
        Icon = new("https://upload.wikimedia.org/wikipedia/commons/thumb/a/a0/Microsoft_Edge_Beta_Icon_%282019%29.svg/240px-Microsoft_Edge_Beta_Icon_%282019%29.svg.png");
        Name = "Open favorites";
        Title = "Edge Beta favorites";
        IsLoading = true;
    }

    public override IListItem[] GetItems()
    {
        if (_items == null)
        {
            _ = Task.Run(GenerateBookmarkItems);
            return [];
        }

        IsLoading = false;
        return _items;
    }

    private void GenerateBookmarkItems()
    {
        var root = EdgeFavoritesApi.FetchAllBookmarks();
        if (root == null)
        {
            _items = [];
            RaiseItemsChanged(0);
            return;
        }

        var items = new List<IListItem>();
        var bookmarksBar = root.Roots.BookmarkBar;
        ProcessBookmarks(bookmarksBar, "Bookmarks bar", items);
        ProcessBookmarks(root.Roots.Other, string.Empty, items);

        _items = items.ToArray();
        RaiseItemsChanged(_items.Length);
    }

    private static void ProcessBookmarks(BookmarkNode? node, string path, List<IListItem> items)
    {
        if (node == null)
        {
            return;
        }

        // If the node has children, process them
        if (node.Children != null)
        {
            foreach (var child in node.Children)
            {
                if (child.Type == "url")
                {
                    var newItem = CreateBookmarkItem(child, path);
                    items.Add(newItem);
                }
                else if (child.Type == "folder")
                {
                    ProcessBookmarks(child, $"{path}/{child.Name}", items);
                }
            }
        }
    }

    private static ListItem CreateBookmarkItem(BookmarkNode node, string path)
    {
        return new ListItem(new OpenUrlCommand(node.Url))
        {
            Title = node.Name,
            Subtitle = path,
        };
    }
}
