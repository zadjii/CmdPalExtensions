// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json;
using System.Threading.Tasks;
using RestSharp;
using Windows.Security.Credentials;

namespace MastodonExtension;

public partial class ApiConfig
{
    public static readonly string PasswordVaultResourceName = "MastodonExtensionKeys";

    public static readonly string PasswordVaultUserCodeName = "UserCodeKey";
    public static readonly string PasswordVaultAppClientId = "AppClientId";
    public static readonly string PasswordVaultAppSecretId = "AppSecretId";

    public static string ClientId { get; private set; } = string.Empty;

    public static string ClientSecret { get; private set; } = string.Empty;

    public static string AppBearerToken { get; private set; } = string.Empty;

    public static string UserBearerToken { get; private set; } = string.Empty;

    // public static string UserAuthorizationCode { get; private set; } = string.Empty;

    // public static bool IsLoggedIn => !string.IsNullOrEmpty(UserAuthorizationCode);
    public static bool HasUserToken => !string.IsNullOrEmpty(UserBearerToken);

    public static bool HasAppId => !string.IsNullOrEmpty(ClientId) && !string.IsNullOrEmpty(ClientSecret);

    static ApiConfig()
    {
        var vault = new PasswordVault();
        try
        {
            var savedClientId = vault.Retrieve(PasswordVaultResourceName, PasswordVaultAppClientId);
            var savedSecretId = vault.Retrieve(PasswordVaultResourceName, PasswordVaultAppSecretId);
            if (savedSecretId != null && savedClientId != null)
            {
                ClientId = savedClientId.Password;
                ClientSecret = savedSecretId.Password;

                var savedClientCode = vault.Retrieve(PasswordVaultResourceName, PasswordVaultUserCodeName);
                if (savedClientCode != null)
                {
                    UserBearerToken = savedClientCode.Password;
                }
            }
        }
        catch (Exception)
        {
            // log?
        }
    }

    // public void SetupApiKeys()
    // {
    //    // See:
    //    // * https://techcommunity.microsoft.com/t5/apps-on-azure-blog/how-to-store-app-secrets-for-your-asp-net-core-project/ba-p/1527531
    //    // * https://stackoverflow.com/a/62972670/1481137
    //    var builder = new ConfigurationBuilder()
    //        .SetBasePath(Directory.GetCurrentDirectory())
    //        .AddUserSecrets<MastodonExtensionActionsProvider>();

    // var config = builder.Build();
    //    var secretProvider = config.Providers.First();
    //    secretProvider.TryGet("keys:client_id", out var client_id);

    // // Todo! probably throw if we fail here
    //    if (client_id == null)
    //    {
    //        throw new InvalidDataException("Somehow, I failed to package the token into the app");
    //    }

    // ClientId = client_id;

    // secretProvider.TryGet("keys:client_secret", out var client_secret);

    // // Todo! probably throw if we fail here
    //    if (client_secret == null)
    //    {
    //        throw new InvalidDataException("Somehow, I failed to package the token into the app");
    //    }

    // ClientSecret = client_secret;
    // }
    public static async Task GetClientIdAndSecret()
    {
        // var options = new RestClientOptions($"https://mastodon.social/oauth/token");
        var client = new RestClient("https://mastodon.social");
        var endpoint = "/api/v1/apps";
        var request = new RestRequest(endpoint, Method.Post);
        request.AddHeader("accept", "application/json");
        request.AddHeader("Content-Type", "application/x-www-form-urlencoded");

        request.AddParameter("client_name", "Mastodon CmdPal Extension");
        request.AddParameter("redirect_uris", "urn:ietf:wg:oauth:2.0:oob");
        request.AddParameter("scopes", "read write push");
        request.AddParameter("website", "https://github.com/zadjii/CmdPalExtensions");

        // request.AddParameter("client_id", $"{ApiConfig.ClientId}");
        // request.AddParameter("client_secret", $"{ApiConfig.ClientSecret}");
        // request.AddParameter("redirect_uri", "urn:ietf:wg:oauth:2.0:oob");
        // request.AddParameter("grant_type", "authorization_code");
        // request.AddParameter("code", $"{authCode}");
        // request.AddParameter("scope", "read write push");
        var response = await client.ExecuteAsync(request);
        var content = response.Content;
        try
        {
            var secrets = JsonSerializer.Deserialize<AppSecrets>(content);
            if (secrets == null)
            {
                // it no worky?

                // ApiConfig.LogOutUser();
            }
            else
            {
                ClientId = secrets.ClientId;
                ClientSecret = secrets.ClientSecret;

                var vault = new PasswordVault();
                var clientIdToken = new PasswordCredential()
                {
                    Resource = ApiConfig.PasswordVaultResourceName,
                    UserName = ApiConfig.PasswordVaultAppClientId,
                    Password = ApiConfig.ClientId,
                };
                vault.Add(clientIdToken);

                var clientSecretToken = new PasswordCredential()
                {
                    Resource = ApiConfig.PasswordVaultResourceName,
                    UserName = ApiConfig.PasswordVaultAppSecretId,
                    Password = ApiConfig.ClientSecret,
                };
                vault.Add(clientSecretToken);
            }
        }
        catch (Exception)
        {
            ApiConfig.LogOutUser();
        }
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
}
