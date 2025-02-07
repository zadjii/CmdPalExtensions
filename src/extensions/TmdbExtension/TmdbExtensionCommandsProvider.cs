// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.Extensions.Configuration;

namespace TmdbExtension;

public partial class TmdbExtensionActionsProvider : CommandProvider
{
    public static string BearerToken { get; private set; } = string.Empty;

    public TmdbExtensionActionsProvider()
    {
        DisplayName = "TMDB Search Commands";
        Icon = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory.ToString(), "Assets\\Tmdb-312x276-logo.png"));

        SetupApiKeys();
    }

    private readonly ICommandItem[] _commands = [
        new CommandItem(new TmdbExtensionPage() { Title = "Search movies on TMDB" }),
    ];

    public override ICommandItem[] TopLevelCommands() => _commands;

    // FOR SETUP
    // ```ps1
    // dotnet user-secrets init
    // dotnet user-secrets set "keys:bearerToken" THE_TOKEN_HERE
    // dotnet user-secrets list
    // ```
    private void SetupApiKeys()
    {
        // See:
        // * https://techcommunity.microsoft.com/t5/apps-on-azure-blog/how-to-store-app-secrets-for-your-asp-net-core-project/ba-p/1527531
        // * https://stackoverflow.com/a/62972670/1481137
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddUserSecrets<TmdbExtensionActionsProvider>();

        var config = builder.Build();
        var secretProvider = config.Providers.First();
        secretProvider.TryGet("keys:bearerToken", out var token);

        // Todo! probably throw if we fail here
        if (token == null)
        {
            throw new InvalidDataException("Somehow, I failed to package the token into the app");
        }

        BearerToken = token;
    }
}
