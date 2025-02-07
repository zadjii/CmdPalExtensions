// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using static EdgeFavoritesExtension.EdgeFavoritesApi;

namespace EdgeFavoritesExtension;

internal sealed partial class EdgeFavoritesExtensionPage : ListPage
{
    private readonly Branding _branding;

    private IListItem[]? _topLevelItems;

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
        if (_topLevelItems == null)
        {
            _ = Task.Run(GenerateBookmarkItems);
            return [];
        }

        IsLoading = false;
        return _topLevelItems;
    }

    private void GenerateBookmarkItems()
    {
        var root = EdgeFavoritesApi.FetchAllBookmarks(_branding);
        if (root == null)
        {
            _topLevelItems = [];
            RaiseItemsChanged(0);
            return;
        }

        var items = new List<IListItem>();
        var bookmarksBar = root.Roots.BookmarkBar;
        ProcessBookmarks(bookmarksBar, "Bookmarks bar/", items);
        ProcessBookmarks(root.Roots.Other, string.Empty, items);

        _topLevelItems = [.. items];
        RaiseItemsChanged(_topLevelItems.Length);
    }

    private void ProcessBookmarks(BookmarkNode? node, string path, List<IListItem> items)
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
                    if (SettingsManager.Instance.FlatList)
                    {
                        ProcessBookmarks(child, $"{path}{child.Name}/", items);
                    }
                    else
                    {
                        items.Add(CreateBookmarkFolderItem(_branding, child, $"{path}{child.Name}/"));
                    }
                }
            }
        }
    }

    public static ListItem CreateBookmarkItem(BookmarkNode node, string path)
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

    public static ListItem CreateBookmarkFolderItem(Branding branding, BookmarkNode node, string path)
    {
        var page = new EdgeFavoritesFolderPage(branding, node, path);
        var newItem = new ListItem(page)
        {
            Icon = new("\uE838"), // FolderOpen
            Title = page.Title,
            Subtitle = $"{node.Children?.Count ?? 0} sub-items",
        };
        return newItem;
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "I just like it")]
internal sealed partial class EdgeFavoritesFolderPage : ListPage
{
    private readonly Branding _branding;
    private readonly BookmarkNode _node;
    private readonly string _nodePath;

    private IListItem[]? _items;

    public EdgeFavoritesFolderPage(Branding branding, BookmarkNode node, string nodePath)
    {
        _branding = branding;
        _node = node;
        _nodePath = nodePath;

        Icon =
            branding switch
            {
                Branding.Stable => new("https://upload.wikimedia.org/wikipedia/commons/thumb/9/98/Microsoft_Edge_logo_%282019%29.svg/240px-Microsoft_Edge_logo_%282019%29.svg.png"),
                Branding.Beta => new("https://upload.wikimedia.org/wikipedia/commons/thumb/a/a0/Microsoft_Edge_Beta_Icon_%282019%29.svg/240px-Microsoft_Edge_Beta_Icon_%282019%29.svg.png"),
                Branding.Canary => new("https://upload.wikimedia.org/wikipedia/commons/thumb/e/e4/Microsoft_Edge_Canary_Logo_%282019%29.svg/240px-Microsoft_Edge_Canary_Logo_%282019%29.svg.png"),
                Branding.Dev => new("https://upload.wikimedia.org/wikipedia/commons/thumb/3/32/Microsoft_Edge_Dev_Icon_%282019%29.svg/240px-Microsoft_Edge_Dev_Icon_%282019%29.svg.png"),
                _ => throw new NotImplementedException(),
            };

        Name = "View";

        // _ = branding switch
        // {
        //    Branding.Stable => "Edge",
        //    Branding.Beta => "Edge Beta",
        //    Branding.Canary => "Edge Canary",
        //    Branding.Dev => "Edge Dev",
        //    _ => throw new NotImplementedException(),
        // };
        Title = node.Name;
        IsLoading = true;
    }

    public override IListItem[] GetItems()
    {
        if (_items == null)
        {
            // Lazy load - only generate the list items when we're first called
            // We don't need to do anything async, because we already loaded
            // the file
            GenerateBookmarkItems();
        }

        IsLoading = false;
        return _items ?? [];
    }

    private void GenerateBookmarkItems()
    {
        var root = _node;
        if (root == null)
        {
            _items = [];
            return;
        }

        var items = new List<IListItem>();
        ProcessBookmarks(_node, $"{_nodePath}{_node.Name}/", items);

        _items = [.. items];
    }

    private void ProcessBookmarks(BookmarkNode? node, string path, List<IListItem> items)
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
                    var newItem = EdgeFavoritesExtensionPage.CreateBookmarkItem(child, path);
                    items.Add(newItem);
                }
                else if (child.Type == "folder")
                {
                    var newItem = EdgeFavoritesExtensionPage.CreateBookmarkFolderItem(_branding, child, path);
                    items.Add(newItem);
                }
            }
        }
    }
}
