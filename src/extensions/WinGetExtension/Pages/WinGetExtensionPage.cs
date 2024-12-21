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
using WindowsPackageManager.Interop;

namespace WinGetExtension;

internal sealed partial class WinGetExtensionPage : DynamicListPage, IDisposable
{
    private readonly WindowsPackageManagerStandardFactory _winGetFactory;
    private readonly PackageManager _manager;
    private readonly IReadOnlyList<PackageCatalogReference> _availableCatalogs;

    private readonly Lock _resultsLock = new();
    private readonly Lock _searchLock = new();
    private CancellationTokenSource? _cancellationTokenSource;

    private IEnumerable<CatalogPackage> _results = [];

    public WinGetExtensionPage()
    {
        Icon = new("\uE74C");
        Name = "Search Winget";

        _winGetFactory = new WindowsPackageManagerStandardFactory();

        // Create Package Manager and get available catalogs
        _manager = _winGetFactory.CreatePackageManager();
        _availableCatalogs = _manager.GetPackageCatalogs();
    }

    public override IListItem[] GetItems()
    {
        IsLoading = false;
        lock (_resultsLock)
        {
            return !_results.Any()
                ? [
                    new ListItem(new NoOpCommand())
                    {
                        Title = string.IsNullOrEmpty(SearchText) ? "Start typing to search for packages" : "No packages found",
                    }
                ]
                : _results.Select(p =>
                new ListItem(new InstallPackageCommand(p))
                {
                    Title = p.Name,
                    Subtitle = p.Id,
                    Tags = [new Tag() { Text = p.AvailableVersions[0].Version }],
                }).ToArray();
        }
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        IsLoading = true;
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

            return;
        }

        _ = Task.Run(async () =>
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
                () =>
            {
                // Were we already canceled?
                currentCts.Token.ThrowIfCancellationRequested();
                return DoSearchAsync(currentCts.Token);
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
        });
    }

    private sealed class PackageIdCompare : IEqualityComparer<CatalogPackage>
    {
        public bool Equals(CatalogPackage? x, CatalogPackage? y) => x?.Id == y?.Id;

        public int GetHashCode([DisallowNull] CatalogPackage obj) => obj.Id.GetHashCode();
    }

    private async Task<IEnumerable<CatalogPackage>> DoSearchAsync(CancellationToken ct)
    {
        var query = SearchText;
        var results = new HashSet<CatalogPackage>(new PackageIdCompare());

        var nameFilter = _winGetFactory.CreatePackageMatchFilter();
        nameFilter.Field = Microsoft.Management.Deployment.PackageMatchField.Name;
        nameFilter.Value = query;

        // filterList.Filters.Add(nameFilter);
        var idFilter = _winGetFactory.CreatePackageMatchFilter();
        idFilter.Field = Microsoft.Management.Deployment.PackageMatchField.Id;
        idFilter.Value = query;
        idFilter.Option = PackageFieldMatchOption.ContainsCaseInsensitive;

        // filterList.Filters.Add(idFilter);
        var monikerFilter = _winGetFactory.CreatePackageMatchFilter();
        monikerFilter.Field = Microsoft.Management.Deployment.PackageMatchField.Moniker;
        monikerFilter.Value = query;
        monikerFilter.Option = PackageFieldMatchOption.ContainsCaseInsensitive;

        var commandFilter = _winGetFactory.CreatePackageMatchFilter();
        commandFilter.Field = Microsoft.Management.Deployment.PackageMatchField.Command;
        commandFilter.Value = query;
        commandFilter.Option = PackageFieldMatchOption.ContainsCaseInsensitive;

        // filterList.Filters.Add(monikerFilter);
        PackageMatchFilter[] filters = [
            nameFilter,
            idFilter,
            commandFilter,
            monikerFilter
          ];
        var filterList = filters.Select(f =>
        {
            var opts = _winGetFactory.CreateFindPackagesOptions();
            opts.Filters.Add(f);
            return opts;
        });
        if (ct.IsCancellationRequested)
        {
            // Clean up here, then...
            ct.ThrowIfCancellationRequested();
        }

        var connections = _availableCatalogs.ToArray().Select(reference => reference.Connect().PackageCatalog);

        foreach (var catalog in connections)
        {
            Debug.WriteLine($"  Searching {catalog.Info.Name} ({query})");

            foreach (var opts in filterList)
            {
                if (ct.IsCancellationRequested)
                {
                    ct.ThrowIfCancellationRequested();
                }

                //// Create a filter to search for packages
                // var filterList = _winGetFactory.CreateFindPackagesOptions();

                //// Add the query to the filter
                // filterList.Filters.Add(filter);

                // Find the packages with the filters
                var searchResults = await catalog/*.Connect().PackageCatalog*/.FindPackagesAsync(opts);
                foreach (var match in searchResults.Matches.ToArray())
                {
                    if (ct.IsCancellationRequested)
                    {
                        ct.ThrowIfCancellationRequested();
                    }

                    // Print the packages
                    var package = match.CatalogPackage;

                    // Console.WriteLine(Package.Name);
                    results.Add(package);
                }

                Debug.WriteLine($"    [{catalog.Info.Name}] ({query}): count: {results.Count}");
            }
        }

        return results;
    }

    public void Dispose() => throw new NotImplementedException();
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
