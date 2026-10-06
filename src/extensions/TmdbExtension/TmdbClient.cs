// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;

namespace TmdbExtension;

internal sealed class TmdbClient
{
    public static TmdbClient Shared { get; } = new(
        new RestClient("https://api.themoviedb.org/3"),
        () => ApiConfig.UserBearerToken,
        () => SettingsManager.Instance.Language);

    private readonly RestClient _client;
    private readonly Func<string> _getToken;
    private readonly Func<string> _getLanguage;

    internal TmdbClient(RestClient client, Func<string> getToken, Func<string> getLanguage)
    {
        _client = client;
        _getToken = getToken;
        _getLanguage = getLanguage;
    }

    public async Task<MovieSearchResult[]> SearchMoviesAsync(string query, CancellationToken cancellationToken)
    {
        var request = new RestRequest("search/movie");
        request.AddQueryParameter("query", query);
        var response = await GetAsync<MovieSearchResponse>(request, cancellationToken);
        return response.Results ?? throw new InvalidDataException("TMDB returned no results collection.");
    }

    public Task<MovieDetailsResponse> GetMovieDetailsAsync(int movieId, CancellationToken cancellationToken = default)
    {
        var request = new RestRequest($"movie/{movieId}");
        request.AddQueryParameter("append_to_response", "watch/providers");
        return GetAsync<MovieDetailsResponse>(request, cancellationToken);
    }

    private async Task<T> GetAsync<T>(RestRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var token = _getToken().Trim();
        if (string.IsNullOrEmpty(token))
        {
            throw new HttpRequestException("A TMDB API Read Access Token is required.", null, HttpStatusCode.Unauthorized);
        }

        request.AddHeader("accept", "application/json");
        request.AddHeader("Authorization", $"Bearer {token}");
        request.AddQueryParameter("language", _getLanguage());

        var response = await _client.ExecuteGetAsync(request, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        if (response.ResponseStatus == ResponseStatus.TimedOut)
        {
            throw new TimeoutException("The TMDB request timed out.");
        }

        if (!response.IsSuccessful)
        {
            throw new HttpRequestException("The TMDB request failed.", response.ErrorException, response.StatusCode == 0 ? null : response.StatusCode);
        }

        if (string.IsNullOrWhiteSpace(response.Content))
        {
            throw new InvalidDataException("TMDB returned an empty response.");
        }

        return JsonSerializer.Deserialize<T>(response.Content)
            ?? throw new InvalidDataException("TMDB returned a null response.");
    }
}
