// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using HtmlAgilityPack;

namespace MastodonExtension;

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "It doesn't matter")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
public class MastodonStatus
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("uri")]
    public string Uri { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("account")]
    public MastodonAccount Account { get; set; }

    [JsonPropertyName("favourites_count")]
    public int Favorites { get; set; }

    [JsonPropertyName("reblogs_count")]
    public int Boosts { get; set; }

    [JsonPropertyName("replies_count")]
    public int Replies { get; set; }

    [JsonPropertyName("favourited")]
    public bool Favorited { get; set; }

    [JsonPropertyName("reblogged")]
    public bool Reblogged { get; set; }

    [JsonPropertyName("media_attachments")]
    public List<MediaAttachment> MediaAttachments { get; set; }

    public string ContentAsPlainText()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(Content);
        var plainTextBuilder = new StringBuilder();
        foreach (var node in doc.DocumentNode.ChildNodes)
        {
            plainTextBuilder.Append(ParseNodeToPlaintext(node));
        }

        return plainTextBuilder.ToString();
    }

    public string ContentAsMarkdown(bool escapeHashtags, bool addMedia)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(Content.Replace("<br>", "\n\n").Replace("<br />", "\n\n"));
        var markdownBuilder = new StringBuilder();
        foreach (var node in doc.DocumentNode.ChildNodes)
        {
            markdownBuilder.Append(ParseNodeToMarkdown(node, escapeHashtags));
        }

        // change this to >1 if you want to try the HeroImage thing
        if (addMedia && MediaAttachments.Count > 0)
        {
            foreach (var mediaAttachment in MediaAttachments)
            {
                // A newline in a img tag blows up the image parser :upside_down:
                var desc = mediaAttachment.Description ?? string.Empty;
                var img = $"\n![{desc.Replace("\n", " ")}]({mediaAttachment.Url})";
                markdownBuilder.Append(img);
            }
        }

        return markdownBuilder.ToString();
    }

    private static string ParseNodeToMarkdown(HtmlNode node, bool escapeHashtags)
    {
        var innerText = escapeHashtags ? node.InnerText.Replace("#", "\\#") : node.InnerText;
        switch (node.Name)
        {
            case "strong":
            case "b":
                return $"**{node.InnerText}**";
            case "em":
            case "i":
                return $"*{node.InnerText}*";
            case "a":
                return $"[{node.InnerText}]({node.GetAttributeValue("href", "#")})";
            case "p":
                return $"{innerText}\n\n";
            case "li":
                return $"{innerText}\n";
            case "#text":
                return innerText;
            default:
                return innerText;  // For unhandled nodes, just return the text.
        }
    }

    private static string ParseNodeToPlaintext(HtmlNode node) => node.InnerText;
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
public class MastodonAccount
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; }

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; }

    [JsonPropertyName("avatar")]
    public string Avatar { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("bot")]
    public bool IsBot { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
public class MediaAttachment
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } // e.g., "image", "video", "gifv", etc.

    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("preview_url")]
    public string PreviewUrl { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
public class MastodonContext
{
    [JsonPropertyName("ancestors")]
    public List<MastodonStatus> Ancestors { get; set; }

    [JsonPropertyName("descendants")]
    public List<MastodonStatus> Descendants { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
public class UserAuthToken
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; }

    [JsonPropertyName("scope")]
    public string Scope { get; set; }

    [JsonPropertyName("created_at")]
    public int CreatedAt { get; set; }
}
