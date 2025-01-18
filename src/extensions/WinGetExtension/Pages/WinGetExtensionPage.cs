// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CmdPal.Extensions;
using Microsoft.CmdPal.Extensions.Helpers;
using Microsoft.Management.Deployment;
using WinGetExtension.Pages;

namespace WinGetExtension;

internal sealed partial class WinGetExtensionPage : DynamicListPage, IDisposable
{
    private readonly string _tag = string.Empty;

    private readonly Lock _resultsLock = new();
    private readonly Lock _searchLock = new();
    private CancellationTokenSource? _cancellationTokenSource;

    private IEnumerable<CatalogPackage>? _results;

    public WinGetExtensionPage(string tag = "")
    {
        Icon = new("\uE74C");
        Name = "Search Winget";
        _tag = tag;
    }

    public override IListItem[] GetItems()
    {
        IListItem[] items = [];
        lock (_resultsLock)
        {
            var emptySearchForTag = _results == null &&
                string.IsNullOrEmpty(SearchText) &&
                !string.IsNullOrEmpty(_tag);

            if (emptySearchForTag)
            {
                IsLoading = true;
                DoSearch(string.Empty);
                return items;
            }

            items = (_results == null || !_results.Any())
                ? [
                    new ListItem(new NoOpCommand())
                    {
                        Title = (string.IsNullOrEmpty(SearchText) && string.IsNullOrEmpty(_tag)) ?
                            "Start typing to search for packages" :
                            "No packages found",
                    }
                ]
                : _results.Select(PackageToListItem).ToArray();
        }

        IsLoading = false;

        return items;
    }

    private static ListItem PackageToListItem(CatalogPackage p)
    {
        var versionText = p.AvailableVersions[0].Version;
        var versionTagText = versionText == "Unknown" && p.AvailableVersions[0].PackageCatalogId == "StoreEdgeFD" ? "msstore" : versionText;

        return new ListItem(new InstallPackageCommand(p))
        {
            Title = p.Name,
            Subtitle = p.Id,
            Tags = [new Tag() { Text = versionTagText }],
        };
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        IsLoading = true;
        if (newSearch == oldSearch)
        {
            return;
        }

        DoSearch(newSearch);
    }

    private void DoSearch(string newSearch)
    {
        if (string.IsNullOrEmpty(newSearch)
            && string.IsNullOrEmpty(_tag))
        {
            lock (_resultsLock)
            {
                this._results = [];
            }

            return;
        }

        _ = Task.Run(CancellableSearchAsync);
    }

    private async Task CancellableSearchAsync()
    {
        CancellationTokenSource? oldCts, currentCts;
        lock (_searchLock)
        {
            oldCts = _cancellationTokenSource;
            currentCts = _cancellationTokenSource = new CancellationTokenSource();
        }

        oldCts?.Cancel();

        var currentSearch = SearchText;
        Debug.WriteLine($"Starting search for '{currentSearch}'");

        var task = Task.Run(
             () => DoSearchAsync(currentCts.Token),
             currentCts.Token);

        try
        {
            var results = await task;
            Debug.WriteLine($"Completed search for '{currentSearch}'");
            lock (_resultsLock)
            {
                this._results = results;
            }

            RaiseItemsChanged(this._results.Count());
        }
        catch (OperationCanceledException)
        {
            // We were cancelled? oh no. Anyways.
            Debug.WriteLine($"Cancelled search for {currentSearch}");
        }
        catch (Exception)
        {
            Debug.WriteLine($"Something else weird happened in {currentSearch}");
        }
        finally
        {
            lock (_searchLock)
            {
                currentCts?.Dispose();
            }

            Debug.WriteLine($"Finally for '{currentSearch}'");
        }
    }

    private async Task<IEnumerable<CatalogPackage>> DoSearchAsync(CancellationToken ct)
    {
        // Were we already canceled?
        ct.ThrowIfCancellationRequested();

        Stopwatch stopwatch = new();
        stopwatch.Start();

        var query = SearchText;
        var results = new HashSet<CatalogPackage>(new PackageIdCompare());

        // Default selector: this is the way to do a `winget search <query>`
        var selector = WinGetStatics.WinGetFactory.CreatePackageMatchFilter();
        selector.Field = Microsoft.Management.Deployment.PackageMatchField.CatalogDefault;
        selector.Value = query;

        var opts = WinGetStatics.WinGetFactory.CreateFindPackagesOptions();
        opts.Selectors.Add(selector);

        // testing
        opts.ResultLimit = 25;

        if (!string.IsNullOrEmpty(_tag))
        {
            var tagFilter = WinGetStatics.WinGetFactory.CreatePackageMatchFilter();
            tagFilter.Field = Microsoft.Management.Deployment.PackageMatchField.Tag;
            tagFilter.Value = query;
            tagFilter.Option = PackageFieldMatchOption.ContainsCaseInsensitive;

            opts.Filters.Add(tagFilter);
        }

        // Clean up here, then...
        ct.ThrowIfCancellationRequested();

        // var connections = WinGetStatics.AvailableCatalogs.ToArray().Select(reference => reference.Connect().PackageCatalog);
        // var connections = WinGetStatics.Connections;
        var catalog = await WinGetStatics.GetCompositeCatalog();

        // foreach (var catalog in connections)
        {
            Debug.WriteLine($"  Searching {catalog.Info.Name} ({query})");

            ct.ThrowIfCancellationRequested();

            // Find the packages with the filters
            var request = catalog.FindPackagesAsync(opts);
            var cancellable = AsTaskWithCancellation(request, ct);
            var searchResults = await cancellable;
            foreach (var match in searchResults.Matches.ToArray())
            {
                ct.ThrowIfCancellationRequested();

                // Print the packages
                var package = match.CatalogPackage;

                // Console.WriteLine(Package.Name);
                results.Add(package);
            }

            Debug.WriteLine($"    [{catalog.Info.Name}] ({query}): count: {results.Count}");
        }

        stopwatch.Stop();

        Debug.WriteLine($"Search \"{query}\" took {stopwatch.ElapsedMilliseconds}ms");

        return results;
    }

    internal async Task<PackageCatalog> GetCompositeCatalog()
    {
        // Get the remote catalog
        // PackageCatalogReference selectedRemoteCatalogRef = _availableCatalogs[0]; // loop?
        // Create the composite catalog
        var createCompositePackageCatalogOptions = WinGetStatics.WinGetFactory.CreateCreateCompositePackageCatalogOptions();

        // createCompositePackageCatalogOptions.Catalogs.Add(selectedRemoteCatalogRef);
        foreach (var catalogReference in WinGetStatics.AvailableCatalogs.ToArray())
        {
            createCompositePackageCatalogOptions.Catalogs.Add(catalogReference);
        }

        createCompositePackageCatalogOptions.CompositeSearchBehavior = CompositeSearchBehavior.RemotePackagesFromAllCatalogs;

        var catalogRef = WinGetStatics.Manager.CreateCompositePackageCatalog(createCompositePackageCatalogOptions);
        var connectResult = await catalogRef.ConnectAsync();
        var compositeCatalog = connectResult.PackageCatalog;
        return compositeCatalog;
    }

    public void Dispose() => throw new NotImplementedException();

    private static async Task<T> AsTaskWithCancellation<T>(Windows.Foundation.IAsyncOperation<T> operation, CancellationToken cancellationToken)
    {
        // Create a TaskCompletionSource to wrap the IAsyncOperation
        var tcs = new TaskCompletionSource<T>();

        // Attach completion and error handlers
        operation.Completed = (op, status) =>
        {
            switch (status)
            {
                case Windows.Foundation.AsyncStatus.Completed:
                    tcs.TrySetResult(op.GetResults());
                    break;
                case Windows.Foundation.AsyncStatus.Error:
                    tcs.TrySetException(op.ErrorCode);
                    break;
                case Windows.Foundation.AsyncStatus.Canceled:
                    tcs.TrySetCanceled();
                    break;
            }
        };

        // Handle cancellation from the CancellationToken
        using (cancellationToken.Register(() =>
        {
            try
            {
                operation.Cancel();
            }
            catch
            {
                // Ignore if cancel fails
            }
        }))
        {
            // Await the TaskCompletionSource's task
            return await tcs.Task;
        }
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "I just like it")]
public sealed class PackageIdCompare : IEqualityComparer<CatalogPackage>
{
    public bool Equals(CatalogPackage? x, CatalogPackage? y) => x?.Id == y?.Id;

    public int GetHashCode([DisallowNull] CatalogPackage obj) => obj.Id.GetHashCode();
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "I just like it")]
public partial class InstallPackageCommand : InvokableCommand
{
    private readonly CatalogPackage _package;

    public InstallPackageCommand(CatalogPackage package)
    {
        _package = package;
        Name = "Install";
        _ = Task.Run(async () =>
        {
            var status = await _package.CheckInstalledStatusAsync();
            var isInstalled = _package.InstalledVersion != null;
            Icon = new(isInstalled ? "\uE930" : "\uE896"); // Completed : Download

            if (status.Status == CheckInstalledStatusResultStatus.Ok)
            {
                var l = status.PackageInstalledStatus;
                _ = l;
            }
        });
    }

    public override ICommandResult Invoke()
    {
        var result = _package.CheckInstalledStatus();
        if (result.Status == CheckInstalledStatusResultStatus.Ok)
        {
            var l = result.PackageInstalledStatus;
            _ = l;
        }

        return CommandResult.KeepOpen();
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "I just like it")]
public partial class InstalledPackagesPage : ListPage
{
    public InstalledPackagesPage()
    {
        Icon = new("\uE74C");
        Name = "Installed Packages";
        IsLoading = true;
    }

    internal async Task<PackageCatalog> GetLocalCatalog()
    {
        var catalogRef = WinGetStatics.Manager.GetLocalPackageCatalog(LocalPackageCatalog.InstalledPackages);
        var connectResult = await catalogRef.ConnectAsync();
        var compositeCatalog = connectResult.PackageCatalog;
        return compositeCatalog;
    }

    public override IListItem[] GetItems()
    {
        var fetchAsync = FetchLocalPackagesAsync();
        fetchAsync.ConfigureAwait(false);
        var results = fetchAsync.Result;
        IListItem[] listItems = !results.Any()
            ? [
                new ListItem(new NoOpCommand())
                    {
                        Title = "No packages found",
                    }
            ]
            : results.Select(p =>
            {
                var versionText = p.InstalledVersion?.Version ?? string.Empty;

                // var versionTagText = versionText == "Unknown" && p.AvailableVersions[0].PackageCatalogId == "StoreEdgeFD" ? "msstore" : versionText;
                Tag[] tags = string.IsNullOrEmpty(versionText) ? [] : [new Tag() { Text = versionText }];
                return new ListItem(new InstallPackageCommand(p))
                {
                    Title = p.Name,
                    Subtitle = p.Id,
                    Tags = tags,
                };
            }).ToArray();
        IsLoading = false;
        return listItems;
    }

    private async Task<IEnumerable<CatalogPackage>> FetchLocalPackagesAsync()
    {
        Stopwatch stopwatch = new();
        stopwatch.Start();

        var results = new HashSet<CatalogPackage>(new PackageIdCompare());

        var catalog = await GetLocalCatalog();
        var opts = WinGetStatics.WinGetFactory.CreateFindPackagesOptions();
        var searchResults = await catalog.FindPackagesAsync(opts);
        foreach (var match in searchResults.Matches.ToArray())
        {
            // Print the packages
            var package = match.CatalogPackage;
            results.Add(package);
        }

        stopwatch.Stop();

        Debug.WriteLine($"Search took {stopwatch.ElapsedMilliseconds}");

        return results;
    }
}
