// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RestSharp;

namespace TmdbExtension.Tests;

[TestClass]
public sealed class TmdbTests
{
    [TestMethod]
    [DataRow("fr", "fr")]
    [DataRow("fr-FR", "fr-FR")]
    [DataRow(" it ", "it")]
    [DataRow("", "en-US")]
    [DataRow("   ", "en-US")]
    public void LanguageSettingIsSavedAndRestored(string value, string expected)
    {
        var path = Path.Combine(Path.GetTempPath(), $"tmdb-settings-test-{Guid.NewGuid()}.json");
        try
        {
            var settings = new SettingsManager(path);
            Assert.AreEqual("en-US", settings.Language);
            var form = (FormContent)settings.Settings.SettingsPage.GetContent()[0];
            var inputs = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                [nameof(SettingsManager.Language)] = value,
            });
            form.SubmitForm(inputs, string.Empty);

            Assert.AreEqual(expected, settings.Language);
            Assert.IsTrue(File.Exists(path), "Changing settings must persist them.");
            Assert.AreEqual(expected, new SettingsManager(path).Language);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task SearchAndDetailsUseCurrentTokenAndLanguage()
    {
        var token = "first-token";
        var language = "fr";
        using var handler = new StubHandler((request, _) =>
        {
            Assert.AreEqual("Bearer", request.Headers.Authorization?.Scheme);
            Assert.AreEqual(token, request.Headers.Authorization?.Parameter);
            var uri = request.RequestUri!;
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            Assert.AreEqual(language, query["language"]);
            Assert.AreEqual(2, query.Count);
            Assert.IsTrue(request.Headers.Accept.ToString().Contains("application/json", StringComparison.Ordinal));
            if (uri.AbsolutePath.EndsWith("/search/movie", StringComparison.Ordinal))
            {
                Assert.AreEqual("Am\u00e9lie & friends? #1", query["query"]);
                return Task.FromResult(Json("""{"results":[{"id":1,"title":"Le Film","release_date":"2025-01-01"}]}"""));
            }

            Assert.AreEqual("/3/movie/1", uri.AbsolutePath);
            Assert.AreEqual("watch/providers", query["append_to_response"]);
            return Task.FromResult(Json("""{"genres":[{"id":1,"name":"Com\u00e9die"}],"watch/providers":{"results":{"US":{"flatrate":[{"provider_name":"Example"}]}}}}"""));
        });
        using var restClient = CreateRestClient(handler);
        var client = new TmdbClient(restClient, () => $"  {token}  ", () => language);

        var movies = await client.SearchMoviesAsync("Am\u00e9lie & friends? #1", CancellationToken.None);
        Assert.AreEqual("Le Film", movies[0].Title);
        Assert.AreEqual("2025", movies[0].ReleaseYear);

        token = "replacement-token";
        language = "it";
        var details = await client.GetMovieDetailsAsync(1);
        Assert.AreEqual("Com\u00e9die", details.Genres[0].Name);
        Assert.AreEqual("Example", details.Providers!.Countries["US"].Flatrate[0].Provider_name);
    }

    [TestMethod]
    [DataRow(HttpStatusCode.Unauthorized)]
    [DataRow(HttpStatusCode.Forbidden)]
    [DataRow(HttpStatusCode.TooManyRequests)]
    [DataRow(HttpStatusCode.InternalServerError)]
    public async Task ApiFailuresAreNotEmptyResults(HttpStatusCode status)
    {
        using var handler = new StubHandler((_, _) => Task.FromResult(Json("""{"success":false,"status_message":"failure"}""", status)));
        using var restClient = CreateRestClient(handler);
        var client = CreateClient(restClient);

        var searchError = await Assert.ThrowsExceptionAsync<HttpRequestException>(() => client.SearchMoviesAsync("movie", CancellationToken.None));
        Assert.AreEqual(status, searchError.StatusCode);
        var detailsError = await Assert.ThrowsExceptionAsync<HttpRequestException>(() => client.GetMovieDetailsAsync(1));
        Assert.AreEqual(status, detailsError.StatusCode);
    }

    [TestMethod]
    public async Task MissingTokenDoesNotSendARequest()
    {
        using var handler = new StubHandler((_, _) => throw new AssertFailedException("A request must not be sent without a token."));
        using var restClient = CreateRestClient(handler);
        var client = new TmdbClient(restClient, () => " ", () => "en-US");

        var exception = await Assert.ThrowsExceptionAsync<HttpRequestException>(() => client.SearchMoviesAsync("movie", CancellationToken.None));
        Assert.AreEqual(HttpStatusCode.Unauthorized, exception.StatusCode);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("null")]
    [DataRow("""{"results":null}""")]
    public async Task EmptyOrNullResponsesFail(string content)
    {
        using var handler = new StubHandler((_, _) => Task.FromResult(Json(content)));
        using var restClient = CreateRestClient(handler);
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => CreateClient(restClient).SearchMoviesAsync("movie", CancellationToken.None));
    }

    [TestMethod]
    [DataRow("{")]
    [DataRow("{}")]
    [DataRow("""{"success":false}""")]
    public async Task InvalidResponsesFail(string content)
    {
        using var handler = new StubHandler((_, _) => Task.FromResult(Json(content)));
        using var restClient = CreateRestClient(handler);
        await Assert.ThrowsExceptionAsync<JsonException>(() => CreateClient(restClient).SearchMoviesAsync("movie", CancellationToken.None));
    }

    [TestMethod]
    public async Task EmptyResultsAreSuccessful()
    {
        using var handler = new StubHandler((_, _) => Task.FromResult(Json("""{"results":[]}""")));
        using var restClient = CreateRestClient(handler);
        var movies = await CreateClient(restClient).SearchMoviesAsync("movie", CancellationToken.None);
        Assert.AreEqual(0, movies.Length);
    }

    [TestMethod]
    public async Task NetworkFailureIsVisible()
    {
        using var handler = new StubHandler((_, _) => throw new HttpRequestException("offline"));
        using var restClient = CreateRestClient(handler);
        using var page = new TmdbExtensionPage(CreateClient(restClient));

        page.UpdateSearchText(string.Empty, "movie");
        await WaitUntilAsync(() => !page.IsLoading);
        Assert.AreEqual("Unable to search TMDB", page.GetItems()[0].Title);
        Assert.AreEqual("Check your connection and try again.", page.GetItems()[0].Subtitle);
    }

    [TestMethod]
    public async Task TimeoutIsVisible()
    {
        using var handler = new StubHandler(async (_, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return Json("""{"results":[]}""");
        });
        using var restClient = new RestClient(new RestClientOptions("https://api.themoviedb.org/3")
        {
            ConfigureMessageHandler = _ => handler,
            Timeout = TimeSpan.FromMilliseconds(50),
        });
        using var page = new TmdbExtensionPage(CreateClient(restClient));

        page.UpdateSearchText(string.Empty, "movie");
        await WaitUntilAsync(() => !page.IsLoading);
        Assert.AreEqual("Unable to search TMDB", page.GetItems()[0].Title);
        Assert.AreEqual("TMDB did not respond in time. Try again.", page.GetItems()[0].Subtitle);
    }

    [TestMethod]
    public async Task CompletedSearchCanBeFollowedByAnotherSearch()
    {
        using var handler = new StubHandler((request, _) =>
        {
            var query = System.Web.HttpUtility.ParseQueryString(request.RequestUri!.Query)["query"];
            return Task.FromResult(Json(JsonSerializer.Serialize(new { results = new[] { new { title = query, release_date = "2025" } } })));
        });
        using var restClient = CreateRestClient(handler);
        using var page = new TmdbExtensionPage(CreateClient(restClient));

        page.UpdateSearchText(string.Empty, "first");
        await WaitUntilAsync(() => !page.IsLoading);
        Assert.AreEqual("first (2025)", page.GetItems()[0].Title);

        page.UpdateSearchText("first", "second");
        await WaitUntilAsync(() => !page.IsLoading);
        Assert.AreEqual("second (2025)", page.GetItems()[0].Title);
    }

    [TestMethod]
    public async Task NewSearchCancelsOldSearchWithoutResettingLoading()
    {
        var first = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken firstToken = default;
        using var handler = new StubHandler((request, ct) =>
        {
            if (request.RequestUri!.Query.Contains("first", StringComparison.Ordinal))
            {
                firstToken = ct;
                return first.Task;
            }

            return second.Task;
        });
        using var restClient = CreateRestClient(handler);
        using var page = new TmdbExtensionPage(CreateClient(restClient));
        page.UpdateSearchText(string.Empty, "first");
        page.UpdateSearchText("first", "second");
        Assert.IsTrue(firstToken.IsCancellationRequested);

        first.SetResult(Json("""{"results":[{"title":"stale"}]}"""));
        await Task.Delay(50);
        Assert.IsTrue(page.IsLoading);
        second.SetResult(Json("""{"results":[{"title":"current","release_date":"2026"}]}"""));
        await WaitUntilAsync(() => !page.IsLoading);
        Assert.AreEqual("current (2026)", page.GetItems()[0].Title);
    }

    [TestMethod]
    public async Task ClearingSearchCancelsPendingResults()
    {
        var response = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken requestToken = default;
        using var handler = new StubHandler((_, ct) =>
        {
            requestToken = ct;
            return response.Task;
        });
        using var restClient = CreateRestClient(handler);
        using var page = new TmdbExtensionPage(CreateClient(restClient));
        page.UpdateSearchText(string.Empty, "movie");
        page.UpdateSearchText("movie", string.Empty);
        Assert.IsTrue(requestToken.IsCancellationRequested);
        Assert.IsFalse(page.IsLoading);

        response.SetResult(Json("""{"results":[{"title":"stale"}]}"""));
        await Task.Delay(50);
        Assert.AreEqual("No results found", page.GetItems()[0].Title);
        Assert.IsFalse(page.IsLoading);
    }

    [TestMethod]
    public async Task AuthenticationErrorsOfferLoginAndSearchCanRecover()
    {
        var status = HttpStatusCode.Unauthorized;
        using var handler = new StubHandler((_, _) => Task.FromResult(Json("""{"results":[]}""", status)));
        using var restClient = CreateRestClient(handler);
        using var page = new TmdbExtensionPage(CreateClient(restClient));
        page.UpdateSearchText(string.Empty, "movie");
        await WaitUntilAsync(() => !page.IsLoading);
        Assert.AreEqual("TMDB authentication failed", page.GetItems()[0].Title);
        Assert.IsInstanceOfType<TmdbLoginPage>(page.GetItems()[0].Command);

        status = HttpStatusCode.OK;
        page.UpdateSearchText("movie", "movie 2");
        await WaitUntilAsync(() => !page.IsLoading);
        Assert.AreEqual("No results found", page.GetItems()[0].Title);
    }

    [TestMethod]
    public async Task DetailsFailureStopsLoadingAndDoesNotRefetchInALoop()
    {
        var requests = 0;
        using var handler = new StubHandler((_, _) =>
        {
            requests++;
            return Task.FromResult(Json("{}", HttpStatusCode.InternalServerError));
        });
        using var restClient = CreateRestClient(handler);
        var page = new TmdbMoviePage(new MovieSearchResult { Id = 1, Title = "Movie" }, CreateClient(restClient));
        page.GetItems();
        await WaitUntilAsync(() => !page.IsLoading);
        Assert.AreEqual("Unable to load movie details", page.GetItems()[0].Title);
        page.GetItems();
        Assert.AreEqual(1, requests);
    }

    [TestMethod]
    public async Task DisposingPageCancelsPendingSearchAndIsRepeatable()
    {
        var response = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken requestToken = default;
        using var handler = new StubHandler((_, ct) =>
        {
            requestToken = ct;
            return response.Task;
        });
        using var restClient = CreateRestClient(handler);
        using var page = new TmdbExtensionPage(CreateClient(restClient));
        page.UpdateSearchText(string.Empty, "movie");
        page.Dispose();
        page.Dispose();
        Assert.IsTrue(requestToken.IsCancellationRequested);
        response.SetResult(Json("""{"results":[]}"""));
        await Task.Delay(50);
        Assert.IsFalse(page.IsLoading);
    }

    private static TmdbClient CreateClient(RestClient restClient) => new(restClient, () => "test-token", () => "en-US");

    private static RestClient CreateRestClient(HttpMessageHandler handler) => new(new RestClientOptions("https://api.themoviedb.org/3")
    {
        ConfigureMessageHandler = _ => handler,
    });

    private static HttpResponseMessage Json(string content, HttpStatusCode status = HttpStatusCode.OK) => new(status)
    {
        Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json"),
    };

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var stopwatch = Stopwatch.StartNew();
        while (!condition())
        {
            Assert.IsTrue(stopwatch.Elapsed < TimeSpan.FromSeconds(5), "Timed out waiting for the page.");
            await Task.Delay(10);
        }
    }

    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => sendAsync(request, cancellationToken);
    }
}
