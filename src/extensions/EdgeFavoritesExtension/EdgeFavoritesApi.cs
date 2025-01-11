// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CmdPal.Extensions.Helpers;

namespace EdgeFavoritesExtension;

public class EdgeFavoritesApi
{
    public static BookmarksRoot? FetchAllBookmarks()
    {
        // Path to the Edge bookmarks file
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var bookmarksPath = Path.Combine(localAppData, @"Microsoft\Edge Beta\User Data\Default\Bookmarks");

        if (!File.Exists(bookmarksPath))
        {
            ExtensionHost.LogMessage(new LogMessage() { Message = "Favorites file not found " });
            return null;
        }

        try
        {
            // Read the JSON content
            var jsonContent = File.ReadAllText(bookmarksPath);

            // Deserialize the JSON into the Root object
            var root = JsonSerializer.Deserialize<BookmarksRoot>(jsonContent);
            return root;
        }
        catch (Exception ex)
        {
            ExtensionHost.LogMessage(new LogMessage() { Message = $"Error reading or parsing bookmarks file: {ex.Message}" });
        }

        return null;
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "I like it")]
public class BookmarksRoot
{
    [JsonPropertyName("roots")]
    public required RootNode Roots { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "I like it")]
public class RootNode
{
    [JsonPropertyName("bookmark_bar")]
    public required BookmarkNode BookmarkBar { get; set; }

    [JsonPropertyName("other")]
    public required BookmarkNode Other { get; set; }

    [JsonPropertyName("synced")]
    public required BookmarkNode Synced { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "I like it")]
public class BookmarkNode
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("children")]
    public List<BookmarkNode>? Children { get; set; }
}
