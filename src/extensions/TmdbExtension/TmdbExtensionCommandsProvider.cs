// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text.Json.Nodes;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Windows.Security.Credentials;

namespace TmdbExtension;

public partial class TmdbExtensionActionsProvider : CommandProvider
{
    public static ApiConfig Config { get; } = new();

    private readonly CommandContextItem _logoutItem;
    private readonly CommandItem _loginItem;
    private readonly CommandItem _searchMoviesItem;

    public TmdbExtensionActionsProvider()
    {
        DisplayName = "TMDB Search Commands";
        Icon = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory.ToString(), "Assets\\Tmdb-312x276-logo.png"));

        _logoutItem = new CommandContextItem(new LogoutCommand())
        {
            Title = "Logout of TMDB",
        };

        _searchMoviesItem = new CommandItem(new TmdbExtensionPage())
        {
            Title = "Search movies on TMDB",
            MoreCommands = [_logoutItem],
        };
        _loginItem = new CommandItem(new TmdbLoginPage())
        {
            Title = "Login to search TMDB for movies",
        };

        ApiConfig.UserTokenChanged += (s, e) => RaiseItemsChanged(1);
    }

    public override ICommandItem[] TopLevelCommands()
    {
        if (ApiConfig.HasUserToken)
        {
            // asdf
            return [_searchMoviesItem];
        }
        else
        {
            // qwer
            return [_loginItem];
        }
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Sample code")]
public partial class ApiConfig
{
    public static readonly string PasswordVaultResourceName = "TmdbExtensionKeys";

    public static readonly string PasswordVaultBearerToken = "BearerToken";

    public static string UserBearerToken { get; private set; } = string.Empty;

    public static bool HasUserToken => !string.IsNullOrEmpty(UserBearerToken);

    public static event EventHandler<string?>? UserTokenChanged;

    static ApiConfig()
    {
        var vault = new PasswordVault();
        try
        {
            var savedBearerToken = vault.Retrieve(PasswordVaultResourceName, PasswordVaultBearerToken);
            if (savedBearerToken != null)
            {
                UserBearerToken = savedBearerToken.Password;
            }
        }
        catch (Exception)
        {
            // log?
        }
    }

    public static void LoginUser(string token)
    {
        ApiConfig.UserBearerToken = token;
        AddToVault(PasswordVaultBearerToken, token);
        UserTokenChanged?.Invoke(null, token);
    }

    public static void LogoutUser()
    {
        if (string.IsNullOrEmpty(ApiConfig.UserBearerToken))
        {
            return;
        }

        var vault = new PasswordVault();
        var userAuthCode = new PasswordCredential()
        {
            Resource = ApiConfig.PasswordVaultResourceName,
            UserName = ApiConfig.PasswordVaultBearerToken,
            Password = ApiConfig.UserBearerToken,
        };
        vault.Remove(userAuthCode);

        ApiConfig.UserBearerToken = string.Empty;

        UserTokenChanged?.Invoke(null, null);
    }

    private static PasswordVault AddToVault(string k, string v, PasswordVault? vault = null)
    {
        vault ??= new PasswordVault();
        var val = new PasswordCredential()
        {
            Resource = ApiConfig.PasswordVaultResourceName,
            UserName = k,
            Password = v,
        };
        vault.Add(val);
        return vault;
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Sample code")]
public partial class LogoutCommand : InvokableCommand
{
    public LogoutCommand()
    {
        Name = "Logout";
        Icon = new("\uF3B1");
    }

    public override ICommandResult Invoke()
    {
        ApiConfig.LogoutUser();
        return CommandResult.GoHome();
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Sample code")]
public partial class TmdbLoginPage : ContentPage
{
    private readonly TmdbLoginForm _loginForm = new();

    public TmdbLoginPage()
    {
        Name = "Open";
        Title = "Login to TMDB";
    }

    public override IContent[] GetContent() => [_loginForm];
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Sample code")]
public partial class TmdbLoginForm : FormContent
{
    public TmdbLoginForm()
    {
        TemplateJson = LoginFormTemplate;
    }

    public override ICommandResult SubmitForm(string inputs)
    {
        var formInput = JsonNode.Parse(inputs)?.AsObject();
        if (formInput?.TryGetPropertyValue("Token", out var code) ?? false)
        {
            if (code != null)
            {
                var codeString = code.ToString();
                ApiConfig.LoginUser(codeString);
            }
        }

        return CommandResult.GoHome();
    }

    private static readonly string LoginFormTemplate = """
{
    "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
    "type": "AdaptiveCard",
    "version": "1.6",
    "body": [
        {
            "type": "TextBlock",
            "size": "Medium",
            "weight": "Bolder",
            "text": " Login to TMDB",
            "horizontalAlignment": "Center",
            "wrap": true,
            "style": "heading"
        },
        {
            "type": "TextBlock",
            "label": "API Token",
            "isRequired": true,
            "errorMessage": "API Token is required",
            "text": "Login on tmdb.com, and paste your \"API Read Access Token\" here"
        },
        {
            "type": "Input.Text",
            "id": "Token",
            "style": "Password",
            "label": "API Token",
            "isRequired": true,
            "errorMessage": "API Token is required"
        }
    ],
    "actions": [
        {
            "type": "Action.OpenUrl",
            "title": "View your API token",
            "url": "https://www.themoviedb.org/settings/api"
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
