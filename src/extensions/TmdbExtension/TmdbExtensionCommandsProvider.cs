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
    private readonly TmdbExtensionPage _searchPage = new();

    public TmdbExtensionActionsProvider()
    {
        DisplayName = "TMDB Search Commands";
        Icon = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory.ToString(), "Assets\\Tmdb-312x276-logo.png"));
        Settings = SettingsManager.Instance.Settings;

        _logoutItem = new CommandContextItem(new LogoutCommand())
        {
            Title = "Logout of TMDB",
        };

        var loginPage = new TmdbLoginPage();
        var settingsItem = new CommandContextItem(Settings.SettingsPage);
        _searchMoviesItem = new CommandItem(_searchPage)
        {
            Title = "Search movies on TMDB",
            MoreCommands = [
                new CommandContextItem(loginPage) { Title = "Update TMDB API token" },
                _logoutItem,
                settingsItem,
            ],
        };
        _loginItem = new CommandItem(loginPage)
        {
            Title = "Login to search TMDB for movies",
            MoreCommands = [settingsItem],
        };

        ApiConfig.UserTokenChanged += OnUserTokenChanged;
        SettingsManager.Instance.Settings.SettingsChanged += RefreshSearch;
    }

    public override ICommandItem[] TopLevelCommands() => ApiConfig.HasUserToken ? [_searchMoviesItem] : [_loginItem];

    private void OnUserTokenChanged(object? sender, string? token)
    {
        _searchPage.Refresh();
        RaiseItemsChanged(1);
    }

    private void RefreshSearch(object? sender, object? args) => _searchPage.Refresh();

    public override void Dispose()
    {
        ApiConfig.UserTokenChanged -= OnUserTokenChanged;
        SettingsManager.Instance.Settings.SettingsChanged -= RefreshSearch;
        _searchPage.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Sample code")]
public partial class ApiConfig
{
    public static readonly string PasswordVaultResourceName = "TmdbExtensionKeys";

    public static readonly string PasswordVaultBearerToken = "BearerToken";

    public static string UserBearerToken { get; private set; } = string.Empty;

    public static bool HasUserToken => !string.IsNullOrWhiteSpace(UserBearerToken);

    public static event EventHandler<string?>? UserTokenChanged;

    static ApiConfig()
    {
        try
        {
            var vault = new PasswordVault();
            var savedBearerToken = vault.Retrieve(PasswordVaultResourceName, PasswordVaultBearerToken);
            savedBearerToken.RetrievePassword();
            UserBearerToken = savedBearerToken.Password.Trim();
        }
        catch (Exception exception) when (exception.HResult == unchecked((int)0x80070490))
        {
            // There is no saved credential on first use.
        }
        catch (Exception exception)
        {
            ExtensionHost.LogMessage($"Unable to read the saved TMDB token: HRESULT {exception.HResult:X8}.");
        }
    }

    public static void LoginUser(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        token = token.Trim();
        AddToVault(PasswordVaultBearerToken, token);
        ApiConfig.UserBearerToken = token;
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
        var token = formInput?["Token"]?.ToString().Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            new ToastStatusMessage("Enter your TMDB API Read Access Token.").Show();
            return CommandResult.KeepOpen();
        }

        try
        {
            ApiConfig.LoginUser(token);
        }
        catch (Exception exception)
        {
            ExtensionHost.LogMessage($"Unable to save the TMDB token: HRESULT {exception.HResult:X8}.");
            new ToastStatusMessage("Unable to save your TMDB token in Windows Credential Manager.").Show();
            return CommandResult.KeepOpen();
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
            "text": "Sign in at themoviedb.org and copy the \"API Read Access Token\" from your API settings. Paste the token here, not your account password or API key. Signing in to the website alone does not sign in to this extension.",
            "wrap": true
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
