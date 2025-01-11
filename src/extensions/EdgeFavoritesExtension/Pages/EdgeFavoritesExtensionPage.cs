// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CmdPal.Extensions;
using Microsoft.CmdPal.Extensions.Helpers;
using static EdgeFavoritesExtension.EdgeFavoritesApi;

namespace EdgeFavoritesExtension;

internal sealed partial class EdgeFavoritesExtensionPage : ListPage
{
    private readonly Branding _branding;

    private IListItem[]? _items;

    public EdgeFavoritesExtensionPage(Branding branding)
    {
        _branding = branding;
        Icon = branding switch
        {
            Branding.Stable => new("https://upload.wikimedia.org/wikipedia/commons/thumb/9/98/Microsoft_Edge_logo_%282019%29.svg/240px-Microsoft_Edge_logo_%282019%29.svg.png"),
            Branding.Beta => new("https://upload.wikimedia.org/wikipedia/commons/thumb/a/a0/Microsoft_Edge_Beta_Icon_%282019%29.svg/240px-Microsoft_Edge_Beta_Icon_%282019%29.svg.png"),
            Branding.Canary => new("https://upload.wikimedia.org/wikipedia/commons/thumb/e/e4/Microsoft_Edge_Canary_Logo_%282019%29.svg/240px-Microsoft_Edge_Canary_Logo_%282019%29.svg.png"),
            Branding.Dev => new("https://upload.wikimedia.org/wikipedia/commons/thumb/3/32/Microsoft_Edge_Dev_Icon_%282019%29.svg/240px-Microsoft_Edge_Dev_Icon_%282019%29.svg.png"),
            _ => throw new NotImplementedException(),
        };

        Name = "Open favorites";

        var brandingName = branding switch
        {
            Branding.Stable => "Edge",
            Branding.Beta => "Edge Beta",
            Branding.Canary => "Edge Canary",
            Branding.Dev => "Edge Dev",
            _ => throw new NotImplementedException(),
        };
        Title = $"{brandingName} favorites";
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
        var root = EdgeFavoritesApi.FetchAllBookmarks(_branding);
        if (root == null)
        {
            _items = [];
            RaiseItemsChanged(0);
            return;
        }

        var items = new List<IListItem>();
        var bookmarksBar = root.Roots.BookmarkBar;
        ProcessBookmarks(bookmarksBar, "Bookmarks bar/", items);
        ProcessBookmarks(root.Roots.Other, string.Empty, items);

        _items = [.. items];
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
                    ProcessBookmarks(child, $"{path}{child.Name}/", items);
                }
            }
        }
    }

    private static ListItem CreateBookmarkItem(BookmarkNode node, string path)
    {
        return new ListItem(new OpenUrlCommand(node.Url))
        {
            Title = node.Name,
            Subtitle = node.Url,
            Details = new Details()
            {
                Body = $"""
# {node.Name}
_{path}_
{node.Url}
""",
            },
        };
    }
}
