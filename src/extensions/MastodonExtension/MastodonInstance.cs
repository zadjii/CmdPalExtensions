// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;

namespace MastodonExtension;

internal sealed class MastodonInstance
{
    internal const string DefaultUrl = "https://mastodon.social";

    internal string Url { get; }

    internal string CredentialResource => Url == DefaultUrl
        ? "MastodonExtensionKeys"
        : $"MastodonExtensionKeys:{Url}";

    private MastodonInstance(string url)
    {
        Url = url;
    }

    internal static MastodonInstance Parse(string? input)
    {
        var value = input?.Trim() ?? string.Empty;
        if (!value.Contains("://", StringComparison.Ordinal))
        {
            value = $"https://{value}";
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || uri.HostNameType == UriHostNameType.Unknown
            || string.IsNullOrEmpty(uri.Host)
            || !string.IsNullOrEmpty(uri.UserInfo)
            || uri.AbsolutePath != "/"
            || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment))
        {
            throw new ArgumentException("Enter a Mastodon hostname or HTTPS instance URL without a path, query, or login information.", nameof(input));
        }

        return new MastodonInstance(uri.GetLeftPart(UriPartial.Authority));
    }

    internal string ApiUrl(string path) => $"{Url}/{path.TrimStart('/')}";

    internal string AuthorizationUrl(string clientId) =>
        $"{ApiUrl("oauth/authorize")}?client_id={Uri.EscapeDataString(clientId)}&scope=read+write+push&redirect_uri=urn:ietf:wg:oauth:2.0:oob&response_type=code";
}
