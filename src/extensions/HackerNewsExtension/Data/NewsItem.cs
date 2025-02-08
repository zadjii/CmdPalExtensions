// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace HackerNewsExtension.Data;

public class NewsItem
{
    [JsonPropertyName("by")]
    public string By { get; set; }

    [JsonPropertyName("descendants")]
    public int Descendants { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("kids")]
    public int[] Kids { get; set; }

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("time")]
    public int Time { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }

    public string CommentsUrl => $"https://news.ycombinator.com/item?id={Id}";

    public bool IsLink => !string.IsNullOrEmpty(Url);

    public bool IsDiscussion => !IsLink;

    public string TargetLink => !string.IsNullOrEmpty(Url) ? Url : CommentsUrl;
}
