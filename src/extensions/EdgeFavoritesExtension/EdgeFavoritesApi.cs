// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace EdgeFavoritesExtension;

public class EdgeFavoritesApi
{
    public enum Branding
    {
        Stable = 0,
        Beta = 1,
        Canary = 2,
        Dev = 3,
    }

    public static string BrandingName(Branding branding)
    {
        return branding switch
        {
            Branding.Stable => "Edge",
            Branding.Beta => "Edge Beta",
            Branding.Canary => "Edge Canary",
            Branding.Dev => "Edge Dev",
            _ => throw new NotImplementedException(),
        };
    }

    public static bool HasBranding(Branding branding) => File.Exists(BookmarksPath(branding));

    public static string BookmarksPath(Branding branding)
    {
        // Path to the Edge bookmarks file
        var brandingPath = branding switch
        {
            Branding.Stable => "Edge",
            Branding.Beta => "Edge Beta",
            Branding.Canary => "Edge Canary",
            Branding.Dev => "Edge Dev",
            _ => throw new NotImplementedException(),
        };

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, $@"Microsoft\{brandingPath}\User Data\Default\Bookmarks");
    }

    public static BookmarksRoot? FetchAllBookmarks(Branding branding)
    {
        if (!HasBranding(branding))
        {
            ExtensionHost.LogMessage(new LogMessage() { Message = "Favorites file not found " });
            return null;
        }

        try
        {
            // Read the JSON content
            var jsonContent = File.ReadAllText(BookmarksPath(branding));

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
