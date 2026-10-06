// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CommandPalette.Extensions.Toolkit;
using RestSharp;
using Windows.Security.Credentials;

namespace MastodonExtension;

public partial class ApiConfig
{
    public static readonly string PasswordVaultUserCodeName = "UserCodeKey";
    public static readonly string PasswordVaultAppClientId = "AppClientId";
    public static readonly string PasswordVaultAppSecretId = "AppSecretId";

    private static readonly SemaphoreSlim RegistrationLock = new(1, 1);
    private static volatile Session _session;

    public static string PasswordVaultResourceName => _session.Instance.CredentialResource;

    public static string ClientId => _session.ClientId;

    public static string ClientSecret => _session.ClientSecret;

    public static string UserBearerToken => _session.UserBearerToken;

    public static bool HasUserToken => !string.IsNullOrEmpty(UserBearerToken);

    public static bool HasAppId => !string.IsNullOrEmpty(ClientId) && !string.IsNullOrEmpty(ClientSecret);

    internal static MastodonInstance Instance => _session.Instance;

    public static event EventHandler<string> UserLoginChanged;

    static ApiConfig()
    {
        _session = LoadSession(SettingsManager.Instance.HomeInstance);
        SettingsManager.Instance.InstanceChanged += (s, e) =>
        {
            _session = LoadSession(SettingsManager.Instance.HomeInstance);
            UserLoginChanged?.Invoke(null, UserBearerToken);
        };
    }

    private static Session LoadSession(MastodonInstance instance)
    {
        var vault = new PasswordVault();
        return new Session(instance)
        {
            ClientId = ReadCredential(vault, instance, PasswordVaultAppClientId),
            ClientSecret = ReadCredential(vault, instance, PasswordVaultAppSecretId),
            UserBearerToken = ReadCredential(vault, instance, PasswordVaultUserCodeName),
        };
    }

    private static string ReadCredential(PasswordVault vault, MastodonInstance instance, string name)
    {
        try
        {
            var credential = vault.Retrieve(instance.CredentialResource, name);
            credential.RetrievePassword();
            return credential.Password;
        }
        catch (COMException e) when (e.HResult == unchecked((int)0x80070490))
        {
            // PasswordVault reports ERROR_NOT_FOUND when the user has not signed in yet.
            return string.Empty;
        }
    }

    private static void SaveCredential(Session session, string name, string value)
    {
        var vault = new PasswordVault();
        vault.Add(new PasswordCredential(session.Instance.CredentialResource, name, value));
    }

    internal static RestClient CreateClient(MastodonInstance instance)
    {
        var session = _session;
        if (instance.Url != session.Instance.Url)
        {
            throw new InvalidOperationException("The Mastodon instance changed. Reopen the page before continuing.");
        }

        var client = new RestClient(new RestClientOptions(instance.Url) { FollowRedirects = false });
        if (!string.IsNullOrEmpty(session.UserBearerToken))
        {
            client.AddDefaultHeader("Authorization", $"Bearer {session.UserBearerToken}");
        }

        return client;
    }

    internal static string AuthorizationUrl(MastodonInstance instance)
    {
        var session = _session;
        if (instance.Url != session.Instance.Url || string.IsNullOrEmpty(session.ClientId))
        {
            throw new InvalidOperationException("Reopen the Mastodon login page to register on the selected instance.");
        }

        return instance.AuthorizationUrl(session.ClientId);
    }

    internal static void SendPostAction(MastodonInstance instance, string postId, string verb)
    {
        using var client = CreateClient(instance);
        var request = new RestRequest($"/api/v1/statuses/{postId}/{verb}", Method.Post);
        request.AddHeader("accept", "application/json");
        var response = client.ExecuteAsync(request).GetAwaiter().GetResult();
        if (!response.IsSuccessful)
        {
            throw new InvalidOperationException($"Could not {verb} the post on {instance.Url}. Check your login and try again.");
        }
    }

    public static async Task GetClientIdAndSecret()
    {
        var session = _session;
        await RegistrationLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (session != _session)
            {
                throw new InvalidOperationException("The Mastodon instance changed. Reopen the login page.");
            }

            if (!string.IsNullOrEmpty(session.ClientId) && !string.IsNullOrEmpty(session.ClientSecret))
            {
                return;
            }

            using var client = new RestClient(new RestClientOptions(session.Instance.Url) { FollowRedirects = false });
            var request = new RestRequest("/api/v1/apps", Method.Post);
            request.AddHeader("accept", "application/json");
            request.AddParameter("client_name", "Mastodon CmdPal Extension");
            request.AddParameter("redirect_uris", "urn:ietf:wg:oauth:2.0:oob");
            request.AddParameter("scopes", "read write push");
            request.AddParameter("website", "https://github.com/zadjii/CmdPalExtensions");

            var response = await client.ExecuteAsync(request).ConfigureAwait(false);
            if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
            {
                throw new InvalidOperationException($"Could not register the Mastodon extension on {session.Instance.Url}. Check the instance and try again.");
            }

            var secrets = JsonSerializer.Deserialize<AppSecrets>(response.Content);
            if (string.IsNullOrEmpty(secrets?.ClientId) || string.IsNullOrEmpty(secrets.ClientSecret))
            {
                throw new InvalidOperationException("The Mastodon instance did not return application credentials.");
            }

            if (session != _session)
            {
                throw new InvalidOperationException("The Mastodon instance changed while registering. Reopen the login page.");
            }

            SaveCredential(session, PasswordVaultAppClientId, secrets.ClientId);
            SaveCredential(session, PasswordVaultAppSecretId, secrets.ClientSecret);
            session.ClientId = secrets.ClientId;
            session.ClientSecret = secrets.ClientSecret;
        }
        finally
        {
            RegistrationLock.Release();
        }
    }

    public static async Task LoginUser(string code, string instanceUrl)
    {
        var session = _session;
        if (instanceUrl != session.Instance.Url)
        {
            throw new InvalidOperationException("The Mastodon instance changed. Reopen the login page to sign in.");
        }

        using var client = new RestClient(new RestClientOptions(session.Instance.Url) { FollowRedirects = false });
        var request = new RestRequest("/oauth/token", Method.Post);
        request.AddHeader("accept", "application/json");
        request.AddParameter("client_id", session.ClientId);
        request.AddParameter("client_secret", session.ClientSecret);
        request.AddParameter("redirect_uri", "urn:ietf:wg:oauth:2.0:oob");
        request.AddParameter("grant_type", "authorization_code");
        request.AddParameter("code", code);
        request.AddParameter("scope", "read write push");

        var response = await client.ExecuteAsync(request).ConfigureAwait(false);
        if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
        {
            throw new InvalidOperationException($"Could not sign in to {session.Instance.Url}. Reopen the browser and try a new authorization code.");
        }

        var token = JsonSerializer.Deserialize<UserAuthToken>(response.Content);
        if (string.IsNullOrEmpty(token?.AccessToken))
        {
            throw new InvalidOperationException("The Mastodon instance did not return an access token.");
        }

        if (session != _session)
        {
            throw new InvalidOperationException("The Mastodon instance changed while signing in. Reopen the login page.");
        }

        SaveCredential(session, PasswordVaultUserCodeName, token.AccessToken);
        session.UserBearerToken = token.AccessToken;
        UserLoginChanged?.Invoke(null, UserBearerToken);
    }

    public static void LogOutUser()
    {
        var session = _session;
        if (string.IsNullOrEmpty(session.UserBearerToken))
        {
            return;
        }

        var vault = new PasswordVault();
        vault.Remove(new PasswordCredential(session.Instance.CredentialResource, PasswordVaultUserCodeName, session.UserBearerToken));
        session.UserBearerToken = string.Empty;
        UserLoginChanged?.Invoke(null, UserBearerToken);
    }

    private sealed class Session
    {
        internal MastodonInstance Instance { get; }

        internal string ClientId { get; set; } = string.Empty;

        internal string ClientSecret { get; set; } = string.Empty;

        internal string UserBearerToken { get; set; } = string.Empty;

        internal Session(MastodonInstance instance)
        {
            Instance = instance;
        }
    }
}
