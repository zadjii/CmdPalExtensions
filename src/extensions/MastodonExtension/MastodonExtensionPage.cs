// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.CmdPal.Extensions;
using Microsoft.CmdPal.Extensions.Helpers;
using Microsoft.Extensions.Configuration;
using RestSharp;
using Windows.Security.Credentials;

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
            var postItem = new ListItem(new MastodonPostPage(p))
            {
                Title = p.Account.DisplayName, // p.ContentAsPlainText(),
                Subtitle = $"@{p.Account.Username}",
                Icon = new(p.Account.Avatar),

                // *
                Tags = [
                    new Tag()
                    {
                        Icon = new("\ue734"), // FavoriteStar
                        Text = p.Favorites.ToString(CultureInfo.CurrentCulture),
                    },
                    new Tag()
                    {
                        Icon = new("\ue8ee"), // RepeatAll
                        Text = p.Boosts.ToString(CultureInfo.CurrentCulture),
                    },
                ], // */
                Details = new Details()
                {
                    // It was a cool idea to have a single image as the HeroImage, but the scaling is terrible
                    // HeroImage = new(p.MediaAttachments.Count == 1 ? p.MediaAttachments[0].Url : string.Empty),
                    Body = p.ContentAsMarkdown(true, true),
                },
                MoreCommands = [
                    new CommandContextItem(new OpenUrlCommand(p.Url) { Name = "Open on web" }),
                ],
            };
            this._items.Add(postItem);
        }
    }

    public override IListItem[] GetItems()
    {
        if (_items.Count == 0)
        {
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
public partial class ApiConfig
{
    public static readonly string PasswordVaultResourceName = "MastodonExtensionKeys";
    public static readonly string PasswordVaultUserCodeName = "UserCodeKey";

    public static string ClientId { get; private set; } = string.Empty;

    public static string ClientSecret { get; private set; } = string.Empty;

    public static string AppBearerToken { get; private set; } = string.Empty;

    public static string UserBearerToken { get; private set; } = string.Empty;

    // public static string UserAuthorizationCode { get; private set; } = string.Empty;

    // public static bool IsLoggedIn => !string.IsNullOrEmpty(UserAuthorizationCode);
    public static bool HasUserToken => !string.IsNullOrEmpty(UserBearerToken);

    public void SetupApiKeys()
    {
        // See:
        // * https://techcommunity.microsoft.com/t5/apps-on-azure-blog/how-to-store-app-secrets-for-your-asp-net-core-project/ba-p/1527531
        // * https://stackoverflow.com/a/62972670/1481137
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddUserSecrets<MastodonExtensionActionsProvider>();

        var config = builder.Build();
        var secretProvider = config.Providers.First();
        secretProvider.TryGet("keys:client_id", out var client_id);

        // Todo! probably throw if we fail here
        if (client_id == null)
        {
            throw new InvalidDataException("Somehow, I failed to package the token into the app");
        }

        ClientId = client_id;

        secretProvider.TryGet("keys:client_secret", out var client_secret);

        // Todo! probably throw if we fail here
        if (client_secret == null)
        {
            throw new InvalidDataException("Somehow, I failed to package the token into the app");
        }

        ClientSecret = client_secret;
    }

    private static async Task GetUserToken(string authCode)
    {
        // var options = new RestClientOptions($"https://mastodon.social/oauth/token");
        var client = new RestClient("https://mastodon.social");
        var endpoint = "/oauth/token";
        var request = new RestRequest(endpoint, Method.Post);
        request.AddHeader("accept", "application/json");
        request.AddHeader("Content-Type", "application/x-www-form-urlencoded");

        request.AddParameter("client_id", $"{ApiConfig.ClientId}");
        request.AddParameter("client_secret", $"{ApiConfig.ClientSecret}");
        request.AddParameter("redirect_uri", "urn:ietf:wg:oauth:2.0:oob");
        request.AddParameter("grant_type", "authorization_code");
        request.AddParameter("code", $"{authCode}");
        request.AddParameter("scope", "read write push");
        var response = await client.ExecuteAsync(request);
        var content = response.Content;
        try
        {
            var authToken = JsonSerializer.Deserialize<UserAuthToken>(content);
            if (authToken == null || authToken.AccessToken == null)
            {
                // it no worky?

                // ApiConfig.LogOutUser();
            }
            else
            {
                ApiConfig.UserBearerToken = authToken.AccessToken;
            }
        }
        catch (Exception)
        {
            ApiConfig.LogOutUser();
        }
    }

    public static async Task LoginUser(string code)
    {
        await GetUserToken(code);

        if (string.IsNullOrEmpty(ApiConfig.UserBearerToken))
        {
            return;
        }

        var vault = new PasswordVault();
        var userToken = new PasswordCredential()
        {
            Resource = ApiConfig.PasswordVaultResourceName,
            UserName = ApiConfig.PasswordVaultUserCodeName,
            Password = ApiConfig.UserBearerToken,
        };
        vault.Add(userToken);
    }

    public static void LogOutUser()
    {
        if (string.IsNullOrEmpty(ApiConfig.UserBearerToken))
        {
            return;
        }

        var vault = new PasswordVault();
        var userAuthCode = new PasswordCredential()
        {
            Resource = ApiConfig.PasswordVaultResourceName,
            UserName = ApiConfig.PasswordVaultUserCodeName,
            Password = ApiConfig.UserBearerToken,
        };
        vault.Remove(userAuthCode);

        UserBearerToken = null;
    }

    static ApiConfig()
    {
        var vault = new PasswordVault();
        try
        {
            var savedClientCode = vault.Retrieve(PasswordVaultResourceName, PasswordVaultUserCodeName);
            if (savedClientCode != null)
            {
                UserBearerToken = savedClientCode.Password;
            }
        }
        catch (Exception)
        {
            // log?
        }
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
public partial class MastodonExtensionActionsProvider : CommandProvider
{
    public static ApiConfig Config { get; } = new();

    private readonly CommandItem _loginItem;
    private readonly CommandItem _exploreItem;
    private readonly CommandItem _homeItem;
    private readonly CommandItem _logoutItem;

    public MastodonExtensionActionsProvider()
    {
        DisplayName = "Mastodon extension for cmdpal Commands";
        Config.SetupApiKeys();

        _loginItem = new CommandItem(new MastodonLoginPage());
        _exploreItem = new CommandItem(new MastodonExtensionPage(isExplorePage: true))
        {
            Title = "Explore Mastodon",
            Subtitle = "Explore top posts on mastodon.social",
        };
        _homeItem = new CommandItem(new MastodonExtensionPage(isExplorePage: false))
        {
            Title = "Mastodon",
            Subtitle = "Posts from users and tags you follow on Mastodon",
        };
        _logoutItem = new CommandItem(new LogoutCommand())
        {
            Subtitle = "Log out of Mastodon",
        };
    }

    public override ICommandItem[] TopLevelCommands()
    {
        if (ApiConfig.HasUserToken)
        {
            return [_homeItem, _exploreItem, _logoutItem];
        }
        else
        {
            ExtensionHost.LogMessage(new LogMessage() { Message = "User was not logged in" });
            return [_loginItem, _exploreItem];
        }
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
