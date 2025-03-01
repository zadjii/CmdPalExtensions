// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using RestSharp;
using Windows.Foundation;

namespace MastodonExtension;

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
internal sealed partial class MastodonExtensionPage : ListPage
{
    public static readonly string ExploreUrl = "https://mastodon.social/api/v1/trends/statuses";
    public static readonly string HomeUrl = "https://mastodon.social/api/v1/timelines/home";

    internal static readonly HttpClient Client = new();
    internal static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    private readonly List<ListItem> _items = [];

    private readonly string _statusesUrl = string.Empty;
    private readonly bool _needsLogin;

    public MastodonExtensionPage(bool isExplorePage = true)
    {
        _statusesUrl = isExplorePage ? ExploreUrl : HomeUrl;
        _needsLogin = !isExplorePage;

        Icon = new("https://mastodon.social/packs/media/icons/android-chrome-36x36-4c61fdb42936428af85afdbf8c6a45a8.png");
        Name = "Mastodon";
        Title = isExplorePage ? "Explore" : "Home";
        ShowDetails = true;
        HasMoreItems = true;
        IsLoading = true;

        // #6364ff
        AccentColor = ColorHelpers.FromRgb(99, 100, 255);
    }

    private void AddPosts(List<MastodonStatus> posts)
    {
        foreach (var p in posts)
        {
            var tags = GetTagsForPost(p);
            var favoritePostCommand = new FavoritePostCommand(p);
            var favPostItem = new CommandContextItem(favoritePostCommand);
            var boostPostCommand = new BoostPostCommand(p);
            var boostPostItem = new CommandContextItem(boostPostCommand);

            var postItem = new ListItem(new MastodonPostPage(p))
            {
                Title = p.Account.DisplayName, // p.ContentAsPlainText(),
                Subtitle = $"@{p.Account.Username}",
                Icon = new IconInfo(p.Account.Avatar),

                // *
                Tags = tags.ToArray(), // */
                Details = new Details()
                {
                    // It was a cool idea to have a single image as the HeroImage, but the scaling is terrible
                    // HeroImage = new(p.MediaAttachments.Count == 1 ? p.MediaAttachments[0].Url : string.Empty),
                    Body = p.ContentAsMarkdown(true, true),
                },
                MoreCommands = [
                    new CommandContextItem(new OpenUrlCommand(p.Url) { Name = "Open on web" }),
                    favPostItem,
                    boostPostItem,
                ],
            };
            favoritePostCommand.FavoritedChanged += (sender, args) =>
            {
                postItem.Tags = GetTagsForPost(p).ToArray();

                // This is to mitigate zadjii-msft/PowerToys#253
                favPostItem.Title = favoritePostCommand.Name;
                favPostItem.Icon = favoritePostCommand.Icon;
            };
            boostPostCommand.BoostedChanged += (sender, args) =>
            {
                postItem.Tags = GetTagsForPost(p).ToArray();

                // This is to mitigate zadjii-msft/PowerToys#253
                boostPostItem.Title = boostPostCommand.Name;
                boostPostItem.Icon = boostPostCommand.Icon;
            };
            this._items.Add(postItem);
        }
    }

    private static List<Tag> GetTagsForPost(MastodonStatus p)
    {
        List<Tag> tags = [];
        tags.Add(new Tag()
        {
            Icon = p.Favorited ? new IconInfo("\uE735") : new IconInfo("\ue734"), // FavoriteStar
            Text = p.Favorites.ToString(CultureInfo.CurrentCulture),
            Foreground = p.Favorited ? ColorHelpers.FromArgb(255, 202, 143, 4) : ColorHelpers.NoColor(),
        });
        tags.Add(new Tag()
        {
            Icon = new IconInfo("\uE8EB"), // Reshare, there is no filled share
            Text = p.Boosts.ToString(CultureInfo.CurrentCulture),
            Foreground = p.Reblogged ? ColorHelpers.FromArgb(255, 111, 112, 199) : ColorHelpers.NoColor(),
        });
        if (p.Replies > 0)
        {
            tags.Add(new Tag()
            {
                Icon = new IconInfo("\uE97A"), // Reply
                Text = p.Replies.ToString(CultureInfo.CurrentCulture),
            });
        }

        return tags;
    }

    public override IListItem[] GetItems()
    {
        if (_items.Count == 0)
        {
            if (_needsLogin & !ApiConfig.HasUserToken)
            {
                this.HasMoreItems = false;
                this._items.Clear();
                var loginPage = new MastodonLoginPage();
                var item = new ListItem(loginPage)
                {
                    Title = "Login to Mastodon",
                    Subtitle = "You need to login before you can view your home timeline",
                };

                // this._items.Add(item);
                return [item];
            }
            else
            {
                this.HasMoreItems = true;
            }

            var postsAsync = FetchExplorePage();
            postsAsync.ConfigureAwait(false);
            var posts = postsAsync.Result;
            this.AddPosts(posts);
        }

        return _items
            .ToArray();
    }

    public override void LoadMore()
    {
        this.IsLoading = true;
        ExtensionHost.LogMessage(new LogMessage() { Message = $"Loading 20 posts, starting with {_items.Count}..." });
        var postsAsync = FetchExplorePage(20, this._items.Count);
        postsAsync.ContinueWith((res) =>
        {
            var posts = postsAsync.Result;
            this.AddPosts(posts);
            ExtensionHost.LogMessage(new LogMessage() { Message = $"... got {posts.Count} new posts" });

            this.IsLoading = false;
            this.RaiseItemsChanged(this._items.Count);
        }).ConfigureAwait(false);
    }

    public async Task<List<MastodonStatus>> FetchExplorePage() => await FetchExplorePage(20, 0);

    public async Task<List<MastodonStatus>> FetchExplorePage(int limit, int offset)
    {
        var statuses = new List<MastodonStatus>();

        if (_needsLogin && !ApiConfig.HasUserToken)
        {
            // TODO! ShowMessage & bail
            return statuses;
        }

        try
        {
            // Make a GET request to the Mastodon trends API endpoint
            var options = new RestClientOptions($"{_statusesUrl}?limit={limit}&offset={offset}");
            var client = new RestClient(options);
            var request = new RestRequest(string.Empty);
            request.AddHeader("accept", "application/json");
            request.AddHeader("Authorization", $"Bearer {ApiConfig.UserBearerToken}");
            var response = await client.GetAsync(request);

            // Read and deserialize the response JSON into a list of MastodonStatus objects
            var responseBody = response.Content;
            statuses = JsonSerializer.Deserialize<List<MastodonStatus>>(responseBody, Options);
        }
        catch (Exception e)
        {
            Console.WriteLine($"An error occurred: {e.Message}");
        }

        IsLoading = false;

        return statuses;
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
public partial class MastodonPostForm : Form
{
    private readonly MastodonStatus post;

    public MastodonPostForm(MastodonStatus post)
    {
        this.post = post;
    }

    public override string DataJson()
    {
        return $$"""
{
    "author_display_name": {{JsonSerializer.Serialize(post.Account.DisplayName)}},
    "author_username": {{JsonSerializer.Serialize(post.Account.Username)}},
    "post_content": {{JsonSerializer.Serialize(post.ContentAsMarkdown(false, false))}},
    "author_avatar_url": "{{post.Account.Avatar}}",
    "timestamp": "2017-02-14T06:08:39Z",
    "post_url": "{{post.Url}}"
}
""";
    }

    public override ICommandResult SubmitForm(string payload) => CommandResult.Dismiss();

    public override string TemplateJson()
    {
        var img_block = string.Empty;
        if (post.MediaAttachments.Count > 0)
        {
            img_block = string.Join(',', post.MediaAttachments
                .Select(media => $$""",{"type": "Image","url":"{{media.Url}}","size": "stretch"}""").ToArray());
        }

        return $$"""
{
    "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
    "type": "AdaptiveCard",
    "version": "1.5",
    "body": [
        {
            "type": "Container",
            "items": [
                {
                    "type": "ColumnSet",
                    "columns": [
                        {
                            "type": "Column",
                            "width": "auto",
                            "items": [
                                {
                                    "type": "Image",
                                    "url": "${author_avatar_url}",
                                    "size": "Medium",
                                    "style": "Person"
                                }
                            ]
                        },
                        {
                            "type": "Column",
                            "width": "stretch",
                            "items": [
                                {
                                    "type": "TextBlock",
                                    "weight": "Bolder",
                                    "wrap": true,
                                    "spacing": "small",
                                    "text": "${author_display_name}"
                                },
                                {
                                    "type": "TextBlock",
                                    "weight": "Lighter",
                                    "wrap": true,
                                    "text": "@${author_username}",
                                    "spacing": "Small",
                                    "isSubtle": true,
                                    "size": "Small"
                                }
                            ]
                        }
                    ]
                },
                {
                    "type": "TextBlock",
                    "text": "${post_content}",
                    "wrap": true
                }{{img_block}}
            ]
        }
    ],
    "actions": [
        {
            "type": "Action.OpenUrl",
            "title": "View on Mastodon",
            "url": "${post_url}"
        }
    ]
}
""";
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
public partial class MastodonPostPage : FormPage
{
    private readonly MastodonStatus post;

    public MastodonPostPage(MastodonStatus post)
    {
        Name = "View post";
        this.post = post;
    }

    public override IForm[] Forms()
    {
        var postsAsync = GetRepliesAsync();
        postsAsync.ConfigureAwait(false);
        var posts = postsAsync.Result;
        return posts.Select(p => new MastodonPostForm(p)).ToArray();
    }

    private async Task<List<MastodonStatus>> GetRepliesAsync()
    {
        // Start with our post...
        var replies = new List<MastodonStatus>([this.post]);
        try
        {
            // Make a GET request to the Mastodon context API endpoint
            var url = $"https://mastodon.social/api/v1/statuses/{post.Id}/context";
            var response = await MastodonExtensionPage.Client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            // Read and deserialize the response JSON into a MastodonContext object
            var responseBody = await response.Content.ReadAsStringAsync();
            var context = JsonSerializer.Deserialize<MastodonContext>(responseBody, MastodonExtensionPage.Options);

            // Extract the list of replies (descendants)
            if (context?.Descendants != null)
            {
                // Add others if we need them
                replies.AddRange(context.Descendants);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"An error occurred: {e.Message}");
        }

        return replies;
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
public partial class FavoritePostCommand : InvokableCommand
{
    private readonly MastodonStatus _post;

    public event TypedEventHandler<FavoritePostCommand, bool> FavoritedChanged;

    public FavoritePostCommand(MastodonStatus post)
    {
        this._post = post;
        UpdateName();
    }

    private void UpdateName()
    {
        if (_post.Favorited)
        {
            this.Name = "Unfavorite";
            this.Icon = new IconInfo("\uE8D9");
        }
        else
        {
            this.Name = "Favorite";
            this.Icon = new IconInfo("\uE735");
        }
    }

    public override ICommandResult Invoke()
    {
        var verb = _post.Favorited ? "unfavourite" : "favourite";

        var client = new RestClient("https://mastodon.social");
        var endpoint = $"/api/v1/statuses/{_post.Id}/{verb}";
        var request = new RestRequest(endpoint, Method.Post);
        request.AddHeader("accept", "application/json");
        request.AddHeader("Authorization", $"Bearer {ApiConfig.UserBearerToken}");

        var task = client.ExecuteAsync(request);
        task.ConfigureAwait(false);
        var response = task.Result;
        var content = response.Content;
        if (response.IsSuccessful)
        {
            _post.Favorited = !_post.Favorited;
            _post.Favorites += _post.Favorited ? 1 : -1;
            UpdateName();
            FavoritedChanged?.Invoke(this, _post.Favorited);
        }

        return CommandResult.KeepOpen();
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
public partial class BoostPostCommand : InvokableCommand
{
    private readonly MastodonStatus _post;

    public event TypedEventHandler<BoostPostCommand, bool> BoostedChanged;

    public BoostPostCommand(MastodonStatus post)
    {
        this._post = post;
        UpdateName();
    }

    private void UpdateName()
    {
        if (_post.Reblogged)
        {
            this.Name = "Unboost";
            this.Icon = new("\uE7A7"); // undo
        }
        else
        {
            this.Name = "Boost";
            this.Icon = new("\uE8EB"); // reshare
        }
    }

    public override ICommandResult Invoke()
    {
        var verb = _post.Reblogged ? "unreblog" : "reblog";

        var client = new RestClient("https://mastodon.social");
        var endpoint = $"/api/v1/statuses/{_post.Id}/{verb}";
        var request = new RestRequest(endpoint, Method.Post);
        request.AddHeader("accept", "application/json");
        request.AddHeader("Authorization", $"Bearer {ApiConfig.UserBearerToken}");

        var task = client.ExecuteAsync(request);
        task.ConfigureAwait(false);
        var response = task.Result;
        var content = response.Content;
        if (response.IsSuccessful)
        {
            _post.Reblogged = !_post.Reblogged;
            _post.Boosts += _post.Reblogged ? 1 : -1;
            UpdateName();
            BoostedChanged?.Invoke(this, _post.Reblogged);
        }

        return CommandResult.KeepOpen();
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
public partial class MastodonLoginForm : Form
{
    public MastodonLoginForm()
    {
    }

    public override ICommandResult SubmitForm(string payload)
    {
        var formInput = JsonNode.Parse(payload)?.AsObject();
        if (formInput.TryGetPropertyValue("Token", out var code))
        {
            var codeString = code.ToString();
            _ = ApiConfig.LoginUser(codeString).ConfigureAwait(false);

            // ApiConfig.UserAuthorizationCode = codeString;
        }

        return CommandResult.GoHome();
    }

    public override string TemplateJson()
    {
        var browserUrl = $"https://mastodon.social/oauth/authorize?client_id={ApiConfig.ClientId}&scope=read+write+push&redirect_uri=urn:ietf:wg:oauth:2.0:oob&response_type=code";

        return $$"""
{
    "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
    "type": "AdaptiveCard",
    "version": "1.6",
    "body": [
        {
            "type": "TextBlock",
            "size": "Medium",
            "weight": "Bolder",
            "text": " Login to Mastodon",
            "horizontalAlignment": "Center",
            "wrap": true,
            "style": "heading"
        },
        {
            "type": "TextBlock",
            "label": "Username",
            "isRequired": true,
            "errorMessage": "Username is required",
            "text": "Login using the browser window, then copy and paste the token into this page."
        },
        {
            "type": "Input.Text",
            "id": "Token",
            "style": "Password",
            "label": "Token",
            "isRequired": true,
            "errorMessage": "Token is required"
        }
    ],
    "actions": [
        {
            "type": "Action.OpenUrl",
            "title": "Open browser to login",
            "url": "{{browserUrl}}"
        },
        {
            "type": "Action.Submit",
            "title": "Login",
            "data": {
                "id": "Token"
            }
        }
    ]
}
""";
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
public partial class MastodonLoginPage : FormPage
{
    public MastodonLoginPage()
    {
        Name = "Login";
        Title = "Login to Mastodon";
        Icon = new("https://mastodon.social/packs/media/icons/android-chrome-36x36-4c61fdb42936428af85afdbf8c6a45a8.png");

        // #6364ff
        AccentColor = ColorHelpers.FromRgb(99, 100, 255);
    }

    public override IForm[] Forms() => [new MastodonLoginForm()];
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
public partial class LogoutCommand : InvokableCommand
{
    public LogoutCommand()
    {
        Name = "Logout";
        Icon = new("\uF3B1");
    }

    public override ICommandResult Invoke()
    {
        ApiConfig.LogOutUser();
        return CommandResult.GoHome();
    }
}
