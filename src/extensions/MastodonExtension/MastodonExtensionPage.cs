// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
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
    public static readonly IconInfo MastodonIcon = new("https://mastodon.social/packs/media/icons/android-chrome-36x36-4c61fdb42936428af85afdbf8c6a45a8.png");

    internal static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    private readonly List<ListItem> _items = [];
    private readonly StatusMessage _errorStatus = new() { State = MessageState.Error };

    private int _version;

    private bool IsHomePage { get; }

    private bool IsExplorePage => !IsHomePage;

    private long _oldestId = long.MaxValue;

    public MastodonExtensionPage(bool isExplorePage = true)
    {
        IsHomePage = !isExplorePage;

        Icon = MastodonIcon;
        Name = "Mastodon";
        Title = isExplorePage ? "Explore" : "Home";
        ShowDetails = true;
        HasMoreItems = true;
        IsLoading = true;

        // #6364ff
        AccentColor = ColorHelpers.FromRgb(99, 100, 255);

        ApiConfig.UserLoginChanged += (s, e) =>
        {
            lock (_items)
            {
                _version++;
                _items.Clear();
                _oldestId = long.MaxValue;
                HasMoreItems = true;
                IsLoading = true;
            }

            ExtensionHost.HideStatus(_errorStatus);
            RaiseItemsChanged(0);
        };
    }

    private void AddPosts(List<MastodonStatus> posts, MastodonInstance instance)
    {
        foreach (var p in posts)
        {
            var tags = GetTagsForPost(p);
            var favoritePostCommand = new FavoritePostCommand(p, instance);
            var favPostItem = new CommandContextItem(favoritePostCommand);
            var boostPostCommand = new BoostPostCommand(p, instance);
            var boostPostItem = new CommandContextItem(boostPostCommand);

            var subtitle = p.IsBoost ?
                $"{p.Account.DisplayName} boosted @{p.RealAccount.Username}" :
                $"@{p.Account.Username}";

            var postItem = new ListItem(new MastodonPostPage(p, instance))
            {
                Title = p.RealAccount.DisplayName,
                Subtitle = subtitle,
                Icon = new IconInfo(p.RealAccount.Avatar),

                Tags = tags.ToArray(),
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
            if (p.IntId < _oldestId)
            {
                _oldestId = p.IntId;
            }
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
        int version;
        lock (_items)
        {
            if (_items.Count > 0)
            {
                return _items.ToArray();
            }

            version = _version;
        }

        if (IsHomePage && !ApiConfig.HasUserToken)
        {
            HasMoreItems = false;
            IsLoading = false;
            return [
                new ListItem(new MastodonLoginPage())
                {
                    Title = "Login to Mastodon",
                    Subtitle = "You need to login before you can view your home timeline",
                },
            ];
        }

        var instance = ApiConfig.Instance;
        var posts = FetchExplorePage(instance, version).GetAwaiter().GetResult();
        lock (_items)
        {
            if (version == _version)
            {
                AddPosts(posts, instance);
                HasMoreItems = posts.Count > 0;
                IsLoading = false;
            }

            return _items.ToArray();
        }
    }

    public override void LoadMore()
    {
        int version;
        MastodonInstance instance;
        lock (_items)
        {
            version = _version;
            instance = ApiConfig.Instance;
            IsLoading = true;
        }

        // Weird CmdPal issue:
        // I originally had this be:
        //
        // var postsAsync = FetchExplorePage(20, this._items.Count);
        // postsAsync.ContinueWith((res) =>
        // {
        //     var posts = postsAsync.Result;
        //     ...
        //     this.RaiseItemsChanged(this._items.Count);
        // }).ConfigureAwait(false);
        //
        // but that weirdly seemed to... hang the CmdPal UI thread if I set a
        // breakpoint in our FetchExplorePage?
        _ = Task.Run(() => LoadMoreAsync(instance, version));
    }

    private async Task LoadMoreAsync(MastodonInstance instance, int version)
    {
        lock (_items)
        {
            if (version != _version)
            {
                return;
            }
        }

        var posts = await FetchExplorePage(instance, version, true).ConfigureAwait(false);
        int count;
        lock (_items)
        {
            if (version != _version)
            {
                return;
            }

            AddPosts(posts, instance);
            HasMoreItems = posts.Count > 0;
            IsLoading = false;
            count = _items.Count;
        }

        ExtensionHost.LogMessage(new LogMessage() { Message = $"... got {posts.Count} new posts" });

        RaiseItemsChanged(count);
    }

    private string PostsUrl(MastodonInstance instance, bool loadMore = false)
    {
        var limit = 20;
        var statusesUrl = instance.ApiUrl(IsExplorePage ? "api/v1/trends/statuses" : "api/v1/timelines/home");
        return IsExplorePage
            ? loadMore ?
                $"{statusesUrl}?limit={limit}&offset={_items.Count}" :
                $"{statusesUrl}?limit={limit}&offset=0"
            : loadMore ?
                $"{statusesUrl}?limit={limit}&max_id={_oldestId}" :
                $"{statusesUrl}?limit={limit}";
    }

    private async Task<List<MastodonStatus>> FetchExplorePage(MastodonInstance instance, int version, bool loadMore = false)
    {
        var statuses = new List<MastodonStatus>();

        if (IsHomePage && !ApiConfig.HasUserToken)
        {
            // TODO! ShowMessage & bail
            return statuses;
        }

        try
        {
            using var client = ApiConfig.CreateClient(instance);
            var request = new RestRequest(PostsUrl(instance, loadMore));
            request.AddHeader("accept", "application/json");
            var response = await client.ExecuteAsync(request).ConfigureAwait(false);
            if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
            {
                throw new InvalidOperationException($"Could not load posts from {instance.Url}. Check the instance and your login.");
            }

            // Read and deserialize the response JSON into a list of MastodonStatus objects
            var responseBody = response.Content;
            statuses = JsonSerializer.Deserialize<List<MastodonStatus>>(responseBody, Options)
                ?? throw new JsonException("The Mastodon instance did not return a list of posts.");
            lock (_items)
            {
                if (version == _version)
                {
                    ExtensionHost.HideStatus(_errorStatus);
                }
            }
        }
        catch (Exception e) when (e is HttpRequestException or JsonException or InvalidOperationException)
        {
            ExtensionHost.LogMessage(new LogMessage() { Message = e.Message });
            lock (_items)
            {
                if (version == _version)
                {
                    _errorStatus.Message = e.Message;
                    ExtensionHost.ShowStatus(_errorStatus, StatusContext.Page);
                }
            }
        }

        return statuses;
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
public partial class MastodonPostForm : FormContent
{
    private readonly MastodonStatus post;

    public MastodonPostForm(MastodonStatus post)
    {
        this.post = post;
    }

    public override string DataJson => $$"""
{
    "author_display_name": {{JsonSerializer.Serialize(post.Account.DisplayName)}},
    "author_username": {{JsonSerializer.Serialize(post.Account.Username)}},
    "post_content": {{JsonSerializer.Serialize(post.ContentAsMarkdown(false, false))}},
    "author_avatar_url": "{{post.Account.Avatar}}",
    "timestamp": "2017-02-14T06:08:39Z",
    "post_url": "{{post.Url}}"
}
""";

    public override ICommandResult SubmitForm(string inputs) => CommandResult.Dismiss();

    public override string TemplateJson
    {
        get
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
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
public partial class MastodonPostPage : ContentPage
{
    private readonly MastodonStatus post;
    private readonly MastodonInstance _instance;

    internal MastodonPostPage(MastodonStatus post, MastodonInstance instance)
    {
        Name = "View post";
        this.post = post;
        _instance = instance;
    }

    public override IContent[] GetContent()
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
            using var client = ApiConfig.CreateClient(_instance);
            var request = new RestRequest($"/api/v1/statuses/{post.Id}/context");
            request.AddHeader("accept", "application/json");
            var response = await client.ExecuteAsync(request).ConfigureAwait(false);
            if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
            {
                throw new InvalidOperationException($"Could not load replies from {_instance.Url}.");
            }

            // Read and deserialize the response JSON into a MastodonContext object
            var responseBody = response.Content;
            var context = JsonSerializer.Deserialize<MastodonContext>(responseBody, MastodonExtensionPage.Options);

            // Extract the list of replies (descendants)
            if (context?.Descendants != null)
            {
                // Add others if we need them
                replies.AddRange(context.Descendants);
            }
        }
        catch (Exception e) when (e is HttpRequestException or JsonException or InvalidOperationException)
        {
            ExtensionHost.LogMessage(new LogMessage() { Message = e.Message });
            ExtensionHost.ShowStatus(new StatusMessage() { Message = e.Message, State = MessageState.Error }, StatusContext.Page);
        }

        return replies;
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
public partial class FavoritePostCommand : InvokableCommand
{
    private readonly MastodonStatus _post;
    private readonly MastodonInstance _instance;

    public event TypedEventHandler<FavoritePostCommand, bool> FavoritedChanged;

    internal FavoritePostCommand(MastodonStatus post, MastodonInstance instance)
    {
        this._post = post;
        _instance = instance;
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

        try
        {
            ApiConfig.SendPostAction(_instance, _post.Id, verb);
            _post.Favorited = !_post.Favorited;
            _post.Favorites += _post.Favorited ? 1 : -1;
            UpdateName();
            FavoritedChanged?.Invoke(this, _post.Favorited);
        }
        catch (Exception e) when (e is HttpRequestException or InvalidOperationException)
        {
            ExtensionHost.LogMessage(new LogMessage() { Message = e.Message });
            return CommandResult.ShowToast(e.Message);
        }

        return CommandResult.KeepOpen();
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
public partial class BoostPostCommand : InvokableCommand
{
    private readonly MastodonStatus _post;
    private readonly MastodonInstance _instance;

    public event TypedEventHandler<BoostPostCommand, bool> BoostedChanged;

    internal BoostPostCommand(MastodonStatus post, MastodonInstance instance)
    {
        this._post = post;
        _instance = instance;
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

        try
        {
            ApiConfig.SendPostAction(_instance, _post.Id, verb);
            _post.Reblogged = !_post.Reblogged;
            _post.Boosts += _post.Reblogged ? 1 : -1;
            UpdateName();
            BoostedChanged?.Invoke(this, _post.Reblogged);
        }
        catch (Exception e) when (e is HttpRequestException or InvalidOperationException)
        {
            ExtensionHost.LogMessage(new LogMessage() { Message = e.Message });
            return CommandResult.ShowToast(e.Message);
        }

        return CommandResult.KeepOpen();
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
public partial class MastodonLoginForm : FormContent
{
    private readonly MastodonInstance _instance;

    public MastodonLoginForm()
    {
        _instance = ApiConfig.Instance;
    }

    public override ICommandResult SubmitForm(string inputs)
    {
        try
        {
            var formInput = JsonNode.Parse(inputs)?.AsObject();
            if (formInput == null || !formInput.TryGetPropertyValue("Token", out var code) || string.IsNullOrWhiteSpace(code?.ToString()))
            {
                return CommandResult.ShowToast("Enter the authorization code from your Mastodon instance.");
            }

            ApiConfig.LoginUser(code.ToString().Trim(), _instance.Url).GetAwaiter().GetResult();
            return CommandResult.GoHome();
        }
        catch (Exception e) when (e is HttpRequestException or JsonException or InvalidOperationException or COMException)
        {
            ExtensionHost.LogMessage(new LogMessage() { Message = e.Message });
            return CommandResult.ShowToast(e.Message);
        }
    }

    public override string TemplateJson
    {
        get
        {
            string browserUrl;
            try
            {
                ApiConfig.GetClientIdAndSecret().GetAwaiter().GetResult();
                browserUrl = ApiConfig.AuthorizationUrl(_instance);
            }
            catch (Exception e) when (e is HttpRequestException or JsonException or InvalidOperationException or COMException)
            {
                ExtensionHost.LogMessage(new LogMessage() { Message = e.Message });
                return $$"""
{
    "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
    "type": "AdaptiveCard",
    "version": "1.6",
    "body": [
        {
            "type": "TextBlock",
            "text": {{JsonSerializer.Serialize(e.Message)}},
            "wrap": true
        }
    ]
}
""";
            }

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
            "text": {{JsonSerializer.Serialize($"Login to {_instance.Url} using the browser window, then copy and paste the authorization code into this page.")}},
            "wrap": true
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
            "url": {{JsonSerializer.Serialize(browserUrl)}}
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
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
public partial class MastodonLoginPage : ContentPage
{
    public MastodonLoginPage()
    {
        Name = "Login";
        Title = "Login to Mastodon";
        Icon = MastodonExtensionPage.MastodonIcon;

        // #6364ff
        AccentColor = ColorHelpers.FromRgb(99, 100, 255);
    }

    public override IContent[] GetContent() => [new MastodonLoginForm()];
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
