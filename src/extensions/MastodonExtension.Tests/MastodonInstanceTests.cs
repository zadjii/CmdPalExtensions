// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MastodonExtension;

[TestClass]
public class MastodonInstanceTests
{
    [TestMethod]
    [DataRow("fosstodon.org", "https://fosstodon.org")]
    [DataRow("https://fosstodon.org", "https://fosstodon.org")]
    [DataRow(" https://FOSSTODON.ORG/ ", "https://fosstodon.org")]
    [DataRow("MASTODON.SOCIAL", "https://mastodon.social")]
    [DataRow("https://mastodon.social:443/", "https://mastodon.social")]
    [DataRow("social.example:8443", "https://social.example:8443")]
    public void NormalizesHostnamesAndHttpsUrls(string input, string expected)
    {
        Assert.AreEqual(expected, MastodonInstance.Parse(input).Url);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("https://")]
    [DataRow("http://fosstodon.org")]
    [DataRow("ftp://fosstodon.org")]
    [DataRow("not a hostname")]
    [DataRow("//fosstodon.org")]
    [DataRow("https://user:password@fosstodon.org")]
    [DataRow("https://fosstodon.org/@user")]
    [DataRow("https://fosstodon.org/api/v1")]
    [DataRow("https://fosstodon.org?instance=other.example")]
    [DataRow("https://fosstodon.org#fragment")]
    public void RejectsInvalidOrNonOriginUrls(string? input)
    {
        Assert.ThrowsException<ArgumentException>(() => MastodonInstance.Parse(input));
    }

    [TestMethod]
    [DataRow("mastodon.social")]
    [DataRow("https://MASTODON.SOCIAL/")]
    [DataRow("https://mastodon.social:443")]
    public void PreservesLegacyCredentialsForDefaultInstance(string input)
    {
        Assert.AreEqual("MastodonExtensionKeys", MastodonInstance.Parse(input).CredentialResource);
    }

    [TestMethod]
    public void ScopesCredentialsToNormalizedInstanceIncludingPort()
    {
        var instance = MastodonInstance.Parse("fosstodon.org");
        var equivalent = MastodonInstance.Parse("https://FOSSTODON.ORG:443/");
        var other = MastodonInstance.Parse("social.example");
        var otherPort = MastodonInstance.Parse("fosstodon.org:8443");

        Assert.AreEqual("MastodonExtensionKeys:https://fosstodon.org", instance.CredentialResource);
        Assert.AreEqual(instance.CredentialResource, equivalent.CredentialResource);
        Assert.AreNotEqual(instance.CredentialResource, other.CredentialResource);
        Assert.AreNotEqual(instance.CredentialResource, otherPort.CredentialResource);
        Assert.AreNotEqual("MastodonExtensionKeys", instance.CredentialResource);
    }

    [TestMethod]
    [DataRow("api/v1/apps")]
    [DataRow("oauth/token")]
    [DataRow("api/v1/trends/statuses")]
    [DataRow("api/v1/timelines/home")]
    [DataRow("api/v1/statuses/123/context")]
    [DataRow("api/v1/statuses/123/favourite")]
    [DataRow("api/v1/statuses/123/unfavourite")]
    [DataRow("api/v1/statuses/123/reblog")]
    [DataRow("api/v1/statuses/123/unreblog")]
    public void BuildsEndpointsOnSelectedInstance(string path)
    {
        var instance = MastodonInstance.Parse("social.example:8443");

        Assert.AreEqual($"https://social.example:8443/{path}", instance.ApiUrl(path));
        Assert.AreEqual(instance.ApiUrl(path), instance.ApiUrl($"/{path}"));
    }

    [TestMethod]
    public void BuildsAuthorizationUrlOnSelectedInstanceWithEscapedClientId()
    {
        var instance = MastodonInstance.Parse("fosstodon.org");
        var url = new Uri(instance.AuthorizationUrl("id&with+reserved=value"));

        Assert.AreEqual("fosstodon.org", url.Host);
        Assert.AreEqual("/oauth/authorize", url.AbsolutePath);
        StringAssert.Contains(url.Query, "client_id=id%26with%2Breserved%3Dvalue&");
        StringAssert.Contains(url.Query, "scope=read+write+push");
        StringAssert.Contains(url.Query, "redirect_uri=urn:ietf:wg:oauth:2.0:oob");
        StringAssert.Contains(url.Query, "response_type=code");
    }
}
