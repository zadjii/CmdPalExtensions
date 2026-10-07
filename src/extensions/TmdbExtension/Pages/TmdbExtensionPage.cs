// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace TmdbExtension;

internal sealed partial class TmdbExtensionPage : DynamicListPage, IDisposable
{
    private readonly Lock _searchLock = new();
    private readonly TmdbClient _client;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _disposed;

    private IListItem[] _results = [];

    public TmdbExtensionPage(TmdbClient? client = null)
    {
        _client = client ?? TmdbClient.Shared;
        Id = "MovieSearch";
        Icon = IconHelpers.FromRelativePath("Assets\\Tmdb-312x276-logo.png");
        Name = "Search Movies";
        ShowDetails = true;
    }

    public override IListItem[] GetItems()
    {
        lock (_searchLock)
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

        StartSearch(newSearch);
    }

    internal void Refresh() => StartSearch(SearchText);

    private void StartSearch(string query)
    {
        lock (_searchLock)
        {
            if (_disposed)
            {
                return;
            }

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;
            _results = [];
            if (string.IsNullOrWhiteSpace(query))
            {
                IsLoading = false;
                RaiseItemsChanged(0);
                return;
            }

            var currentCts = _cancellationTokenSource = new CancellationTokenSource();
            IsLoading = true;
            _ = SearchAsync(query, currentCts);
        }
    }

    private async Task SearchAsync(string query, CancellationTokenSource currentCts)
    {
        try
        {
            var results = await DoSearchAsync(query, currentCts.Token);
            lock (_searchLock)
            {
                if (_cancellationTokenSource == currentCts)
                {
                    _results = results;
                    RaiseItemsChanged(_results.Length);
                }
            }
        }
        catch (OperationCanceledException) when (currentCts.IsCancellationRequested)
        {
            // A newer query or disposal owns the page now.
        }
        catch (Exception exception)
        {
            lock (_searchLock)
            {
                if (_cancellationTokenSource == currentCts)
                {
                    _results = [TmdbError.CreateItem("Unable to search TMDB", exception)];
                    RaiseItemsChanged(_results.Length);
                }
            }
        }
        finally
        {
            lock (_searchLock)
            {
                if (_cancellationTokenSource == currentCts)
                {
                    _cancellationTokenSource = null;
                    IsLoading = false;
                }

                currentCts.Dispose();
            }
        }
    }

    private async Task<IListItem[]> DoSearchAsync(string query, CancellationToken ct)
    {
        var movies = await _client.SearchMoviesAsync(query, ct);
        var r = movies.Select(m =>
        {
            ct.ThrowIfCancellationRequested();

            var moviePage = new TmdbMoviePage(m, _client);
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
        lock (_searchLock)
        {
            _disposed = true;
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;
            IsLoading = false;
        }
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
internal sealed partial class TmdbMoviePage : ListPage
{
    private readonly MovieSearchResult _movie;
    private readonly TmdbClient _client;
    private readonly Lock _loadLock = new();
    private bool _loadStarted;
    private IListItem[] _results = [];

    public Details Details { get; private set; }

    public TmdbMoviePage(MovieSearchResult movie, TmdbClient client)
    {
        _movie = movie;
        _client = client;
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
        lock (_loadLock)
        {
            if (!_loadStarted)
            {
                _loadStarted = true;
                _ = LoadDetailsAsync();
            }

            return _results;
        }
    }

    private async Task LoadDetailsAsync()
    {
        IsLoading = true;
        try
        {
            var movieDetails = await _client.GetMovieDetailsAsync(_movie.Id);
            var items = CreateItems(movieDetails);
            lock (_loadLock)
            {
                _results = items;
            }
        }
        catch (Exception exception)
        {
            lock (_loadLock)
            {
                _results = [TmdbError.CreateItem("Unable to load movie details", exception)];
            }
        }
        finally
        {
            IsLoading = false;
            RaiseItemsChanged(_results.Length);
        }
    }

    private IListItem[] CreateItems(MovieDetailsResponse movieDetails)
    {
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

        return items.ToArray();
    }
}
