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

    private readonly CommandItem _loginItem;
    private readonly CommandItem _searchMoviesItem;

    public TmdbExtensionActionsProvider()
    {
        DisplayName = "TMDB Search Commands";
        Icon = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory.ToString(), "Assets\\Tmdb-312x276-logo.png"));

        _searchMoviesItem = new CommandItem(new TmdbExtensionPage()) { Title = "Search movies on TMDB" };
        _loginItem = new CommandItem(new TmdbLoginPage()) { Title = "Login to search TMDB for movies" };
    }

    // private readonly ICommandItem[] _commands = [
    //    new CommandItem(new TmdbExtensionPage() { Title = "Search movies on TMDB" }),
    // ];
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

    //// FOR SETUP
    //// ```ps1
    //// dotnet user-secrets init
    //// dotnet user-secrets set "keys:bearerToken" THE_TOKEN_HERE
    //// dotnet user-secrets list
    //// ```
    // private void SetupApiKeys()
    // {
    //    // See:
    //    // * https://techcommunity.microsoft.com/t5/apps-on-azure-blog/how-to-store-app-secrets-for-your-asp-net-core-project/ba-p/1527531
    //    // * https://stackoverflow.com/a/62972670/1481137
    //    var builder = new ConfigurationBuilder()
    //        .SetBasePath(Directory.GetCurrentDirectory())
    //        .AddUserSecrets<TmdbExtensionActionsProvider>();

    // var config = builder.Build();
    //    var secretProvider = config.Providers.First();
    //    secretProvider.TryGet("keys:bearerToken", out var token);

    // // Todo! probably throw if we fail here
    //    if (token == null)
    //    {
    //        throw new InvalidDataException("Somehow, I failed to package the token into the app");
    //    }

    // BearerToken = token;
    // }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Sample code")]
public partial class ApiConfig
{
    public static readonly string PasswordVaultResourceName = "TmdbExtensionKeys";

    public static readonly string PasswordVaultBearerToken = "BearerToken";

    public static string UserBearerToken { get; private set; } = string.Empty;

    public static bool HasUserToken => !string.IsNullOrEmpty(UserBearerToken);

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

    public static void LoginUser(string token) => AddToVault(PasswordVaultBearerToken, token);

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
