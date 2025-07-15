// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using HackerNewsExtension.Data;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace HackerNewsExtension;

internal sealed partial class HackerNewsPage : ListPage, IDisposable
{
    private readonly HttpClient _httpClient = new();
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    public static readonly IconInfo HackerNewsIcon = new("https://news.ycombinator.com/favicon.ico");
    private static readonly IconInfo CommentsIcon = new("\uE8F2");
    private static readonly IconInfo PostIcon = new("\uE8A1");
    private readonly List<ListItem> _lastPosts = [];
    private DateTime _lastFetch = DateTime.MinValue;

    public HackerNewsPage()
    {
        Icon = HackerNewsIcon;
        Name = "Hacker News";
        AccentColor = ColorHelpers.FromRgb(255, 102, 0);
        IsLoading = true;
        ShowDetails = true;
    }

    public override IListItem[] GetItems()
    {
        var delta = DateTime.UtcNow - _lastFetch;
        if (_lastPosts.Count == 0 || delta.Minutes > 5)
        {
            var t = FetchItems();
            t.ConfigureAwait(false);
            t.Wait();
        }

        IsLoading = false;
        return [.. _lastPosts];
    }

    private async Task FetchItems()
    {
        _lastPosts.Clear();
        _lastFetch = DateTime.UtcNow;

        // Fetch the list of top story IDs from Hacker News
        var topStoriesUrl = "https://hacker-news.firebaseio.com/v0/topstories.json";
        var topStoriesJson = await _httpClient.GetStringAsync(topStoriesUrl);

        // Deserialize the JSON array into a List of integers (story IDs)
        var topStoryIds = JsonSerializer.Deserialize<List<int>>(topStoriesJson);
        var storiesToFetch = 25;
        for (var i = 0; i < Math.Min(storiesToFetch, topStoryIds.Count); i++)
        {
            var storyId = topStoryIds[i];
            var storyUrl = $"https://hacker-news.firebaseio.com/v0/item/{storyId}.json";
            try
            {
                var storyJson = await _httpClient.GetStringAsync(storyUrl);

                // Deserialize the JSON into our Story object.
                // The option PropertyNameCaseInsensitive = true makes sure that
                // properties like "descendants" and "score" are properly bound.
                var story = JsonSerializer.Deserialize<NewsItem>(storyJson, _jsonOptions);
                if (story != null)
                {
                    var targetCommand = new OpenUrlCommand(story.Url) { Name = "Open", Result = CommandResult.Dismiss() };
                    var commentsCommand =
                        new OpenUrlCommand(story.CommentsUrl) { Name = "View comments", Icon = CommentsIcon, Result = CommandResult.Dismiss() };

                    ICommandContextItem[] contextMenu = [];
                    ICommand primary;

                    if (story.IsLink)
                    {
                        primary = targetCommand;
                        contextMenu = [new CommandContextItem(commentsCommand)];
                    }
                    else
                    {
                        primary = commentsCommand;
                    }

                    // var icon = story.IsLink ?
                    //    await GetPostIconFromUrl(story.Url)
                    //    : CommentsIcon;
                    var item = new ListItem(primary)
                    {
                        Title = story.Title,
                        Subtitle = story.TargetLink,

                        // Icon = icon,
                        Tags =
                        [
                            new Tag($"{story.Score} points"),
                            new Tag($"{story.Descendants}") { Icon = CommentsIcon }, new Tag($"by {story.By}"),
                        ],
                        MoreCommands = contextMenu,
                    };
                    _ = Task.Run(async () =>
                    {
                        var icon = story.IsLink ? await GetPostIconFromUrl(story.Url) : CommentsIcon;
                        item.Icon = icon;
                    });
                    _lastPosts.Add(item);
                }
            }
            catch (Exception ex)
            {
                ExtensionHost.LogMessage($"Error fetching story with ID {storyId}: {ex.Message}");
            }
        }
    }

    public void Dispose() => throw new NotImplementedException();

    internal static Uri GetUri(string url)
    {
        Uri uri;
        if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
        {
            if (!Uri.TryCreate("https://" + url, UriKind.Absolute, out uri))
            {
                return null;
            }
        }

        return uri;
    }

    internal async Task<IconInfo> GetPostIconFromUrl(string baseUrl)
    {
        try
        {
            var uri = GetUri(baseUrl);
            if (uri != null)
            {
                var hostname = uri.Host;
                var faviconUrl = $"{uri.Scheme}://{hostname}/favicon.ico";
                var exists = await FaviconExistsAsync(faviconUrl);
                return exists ? new IconInfo(faviconUrl) : PostIcon;
            }
        }
        catch (UriFormatException)
        {
        }

        return PostIcon;
    }

    private async Task<bool> FaviconExistsAsync(string faviconUrl)
    {
        // Prepare a HEAD request message
        var request = new HttpRequestMessage(HttpMethod.Head, faviconUrl);
        try
        {
            // Send the request asynchronously
            var response = await _httpClient.SendAsync(request);

            // Check if the response status indicates success (i.e. favicon exists)
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            // An exception here likely means that the favicon was not found or another error occurred
            return false;
        }
    }
}
