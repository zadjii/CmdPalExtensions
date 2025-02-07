// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using RestSharp;

namespace TmdbExtension;

internal sealed partial class TmdbExtensionPage : DynamicListPage, IDisposable
{
    private readonly Lock _resultsLock = new();
    private readonly Lock _searchLock = new();
    private CancellationTokenSource? _cancellationTokenSource;

    private IListItem[] _results = [];

    public TmdbExtensionPage()
    {
        Icon = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory.ToString(), "Assets\\Tmdb-312x276-logo.png"));
        Name = "Search Movies";
        ShowDetails = true;
    }

    public override IListItem[] GetItems()
    {
        lock (_resultsLock)
        {
            return _results.Length > 0 ? _results : [
                new ListItem(new NoOpCommand()) { Title = "No results found" }
            ];
        }
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        if (newSearch == oldSearch)
        {
            return;
        }

        if (string.IsNullOrEmpty(newSearch))
        {
            lock (_resultsLock)
            {
                this._results = [];
            }

            RaiseItemsChanged(_results.Length);
            return;
        }

        // _ = Task.Run(async () => await DoSearchAsync(newSearch));
        _ = Task.Run(async () =>
        {
            CancellationTokenSource? oldCts, currentCts;
            lock (_searchLock)
            {
                oldCts = _cancellationTokenSource;
                currentCts = _cancellationTokenSource = new CancellationTokenSource();
            }

            IsLoading = true;
            oldCts?.Cancel();
            var currentSearch = SearchText;
            Debug.WriteLine($"Starting search for '{currentSearch}'");
            var task = Task.Run(
                () =>
                {
                    // Were we already canceled?
                    currentCts.Token.ThrowIfCancellationRequested();
                    return DoSearchAsync(newSearch, currentCts.Token);
                },
                currentCts.Token);

            try
            {
                var results = await task;
                Debug.WriteLine($"Completed search for '{currentSearch}'");
                lock (_resultsLock)
                {
                    this._results = results;
                }

                IsLoading = false;

                RaiseItemsChanged(this._results.Length);
            }
            catch (OperationCanceledException)
            {
                // We were cancelled? oh no. Anyways.
                Debug.WriteLine($"Cancelled search for {currentSearch}");
            }
            catch (Exception)
            {
                Debug.WriteLine($"Something else weird happened in {currentSearch}");
                IsLoading = false;
            }
            finally
            {
                lock (_searchLock)
                {
                    currentCts?.Dispose();
                }

                Debug.WriteLine($"Finally for '{currentSearch}'");
            }
        });
    }

    private async Task<IListItem[]> DoSearchAsync(string query, CancellationToken ct)
    {
        // IsLoading = true;
        var options = new RestClientOptions($"https://api.themoviedb.org/3/search/movie?query={Uri.EscapeDataString(query)}");
        var client = new RestClient(options);
        var request = new RestRequest(string.Empty);
        request.AddHeader("accept", "application/json");
        request.AddHeader("Authorization", $"Bearer {TmdbExtensionActionsProvider.BearerToken}");

        ct.ThrowIfCancellationRequested();

        var response = await client.GetAsync(request, cancellationToken: ct);
        var content = response.Content;

        if (string.IsNullOrEmpty(content))
        {
            // IsLoading = false;
            return [];
        }

        var json = JsonSerializer.Deserialize<MovieSearchResponse>(content);
        if (json == null)
        {
            // IsLoading = false;
            throw new InvalidDataException("Somehow got null data from the movie search");
        }

        var movies = json.Results;
        var r = movies.Select(m =>
        {
            ct.ThrowIfCancellationRequested();

            var moviePage = new TmdbMoviePage(m);
            return new ListItem(moviePage)
            {
                Title = $"{m.Title} ({m.ReleaseYear})",
                Tags = [new Tag() { Text = $"{m.Vote_average:0.0}/10" }],
                Details = moviePage.Details,
            };
        }).ToArray();

        return r;
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
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
            Title = _movie.Title,
            Body = _movie.Overview ?? string.Empty,
            HeroImage = new IconInfo($"https://image.tmdb.org/t/p/w92/{_movie.Poster_path}"),
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

        if (string.IsNullOrEmpty(content))
        {
            IsLoading = false;
            return;
        }

        var movieDetails = JsonSerializer.Deserialize<MovieDetailsResponse>(content);

        if (movieDetails == null)
        {
            IsLoading = false;
            throw new InvalidDataException("Somehow got null data from the movie details");
        }

        Details = new Details()
        {
            Title = _movie.Title,
            Body = _movie.Overview ?? string.Empty,
            HeroImage = new IconInfo($"https://image.tmdb.org/t/p/w92/{_movie.Poster_path}"),
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

        if (movieDetails.Providers != null
            && movieDetails.Providers.Countries.TryGetValue("US", out var us))
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
                        Icon = new IconInfo($"https://image.tmdb.org/t/p/w92/{item.Logo_path}"),
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
