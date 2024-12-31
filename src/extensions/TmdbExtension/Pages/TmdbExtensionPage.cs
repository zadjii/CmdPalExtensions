// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.CmdPal.Extensions;
using Microsoft.CmdPal.Extensions.Helpers;
using RestSharp;

namespace TmdbExtension;

internal sealed partial class TmdbExtensionPage : DynamicListPage
{
    private IListItem[] _results = [];

    public TmdbExtensionPage()
    {
        Icon = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory.ToString(), "Assets\\Tmdb-312x276-logo.png"));
        Name = "Search Movies";
        ShowDetails = true;
    }

    public override IListItem[] GetItems()
    {
        return _results.Length > 0 ? _results : [
            new ListItem(new NoOpCommand()) { Title = "No results found" }
        ];
    }

    public override void UpdateSearchText(string oldSearch, string newSearch) => _ = Task.Run(async () => await DoSearchAsync(newSearch));

    private async Task DoSearchAsync(string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            _results = [];
            RaiseItemsChanged(_results.Length);
            return;
        }

        IsLoading = true;
        var options = new RestClientOptions($"https://api.themoviedb.org/3/search/movie?query={Uri.EscapeDataString(query)}");
        var client = new RestClient(options);
        var request = new RestRequest(string.Empty);
        request.AddHeader("accept", "application/json");
        request.AddHeader("Authorization", $"Bearer {TmdbExtensionActionsProvider.BearerToken}");
        var response = await client.GetAsync(request);
        var content = response.Content;

        _ = content;
        var json = JsonSerializer.Deserialize<MovieSearchResponse>(content);

        var movies = json.Results;
        _results = movies.Select(m =>
        {
            var moviePage = new TmdbMoviePage(m);
            return new ListItem(moviePage)
            {
                Title = $"{m.Title} ({m.ReleaseYear})",
                Tags = [new Tag() { Text = $"{m.Vote_average:0.0}/10" }],
                Details = moviePage.Details,
            };
        }).ToArray();
        IsLoading = false;
        RaiseItemsChanged(_results.Length);

        // Once we get the ID
        // https://api.themoviedb.org/3/movie/{movie_id}?append_to_response=watch%2Fproviders
        // var options = new RestClientOptions("https://api.themoviedb.org/3/movie/13669/watch/providers");
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
internal sealed partial class TmdbMoviePage : ListPage
{
    private readonly MovieSearchResult _movie;
    private IListItem[] _results = [];

    public Details Details { get; private set; }

    public TmdbMoviePage(MovieSearchResult movie)
    {
        _movie = movie;
        Name = "View";
        ShowDetails = true;

        Icon = new($"https://image.tmdb.org/t/p/w92/{movie.Poster_path}");
        Title = $"{_movie.Title} ({_movie.ReleaseYear})";

        Details = new Details()
        {
            Body = _movie.Overview ?? string.Empty,
            HeroImage = new($"https://image.tmdb.org/t/p/w92/{_movie.Poster_path}"),
        };
    }

    public override IListItem[] GetItems()
    {
        if (_results.Length == 0)
        {
            _ = DoSearchAsync();
            return [];
        }

        return _results;
    }

    private async Task DoSearchAsync()
    {
        IsLoading = true;
        var options = new RestClientOptions($"https://api.themoviedb.org/3/movie/{_movie.Id}?append_to_response=watch%2Fproviders");
        var client = new RestClient(options);
        var request = new RestRequest(string.Empty);
        request.AddHeader("accept", "application/json");
        request.AddHeader("Authorization", $"Bearer {TmdbExtensionActionsProvider.BearerToken}");
        var response = await client.GetAsync(request);
        var content = response.Content;

        var movieDetails = JsonSerializer.Deserialize<MovieDetailsResponse>(content);

        Details = new Details()
        {
            Body = _movie.Overview ?? string.Empty,
            HeroImage = new($"https://image.tmdb.org/t/p/w92/{_movie.Poster_path}"),
            Metadata = [new DetailsElement() { Key = "Genre", Data = new DetailsTags() { Tags = movieDetails.Genres.Select(g => new Tag() { Text = g.Name }).ToArray() } }],
        };

        List<IListItem> items = [];
        var openOnTmdb = new ListItem(
            new OpenUrlCommand($"https://www.themoviedb.org/movie/{_movie.Id}")
            {
                Icon = new("https://www.themoviedb.org/favicon.ico"),
            })
        {
            Title = $"View on TMDB",
            Details = Details,
        };
        items.Add(openOnTmdb);

        if (movieDetails.Providers.Countries.TryGetValue("US", out var us))
        {
            var link = us.Link;
            var viewStreams = new ListItem(new OpenUrlCommand(link)
            {
                Icon = new("https://www.justwatch.com/favicon.ico"),
            })
            {
                Title = $"View stream links",
                Subtitle = $"Streaming links provided from JustWatch.com",
                Details = Details,
            };
            items.Add(viewStreams);
            Dictionary<string, StreamingProvider[]> all = new()
            {
                { "Streaming", us.Flatrate },
                { "Buy", us.Buy },
                { "Rent", us.Rent },
            };

            foreach (var keyValue in all)
            {
                var tag = new Tag() { Text = keyValue.Key };
                foreach (var item in keyValue.Value)
                {
                    // In reality we should be using
                    // https://developer.themoviedb.org/reference/configuration-details
                    // to get image paths, but eh
                    var li = new ListItem(new NoOpCommand())
                    {
                        Title = item.Provider_name,
                        Icon = new($"https://image.tmdb.org/t/p/w92/{item.Logo_path}"),
                        Tags = [tag],
                        Details = Details,
                    };

                    items.Add(li);
                }
            }
        }

        _results = items.ToArray();
        IsLoading = false;
        RaiseItemsChanged(_results.Length);

        // Once we get the ID
        // https://api.themoviedb.org/3/movie/{movie_id}?append_to_response=watch%2Fproviders
        // var options = new RestClientOptions("https://api.themoviedb.org/3/movie/13669/watch/providers");
    }
}
