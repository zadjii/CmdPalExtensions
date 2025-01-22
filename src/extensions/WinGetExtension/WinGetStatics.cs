﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Management.Deployment;
using WindowsPackageManager.Interop;

namespace WinGetExtension;

internal static class WinGetStatics
{
    public static WindowsPackageManagerStandardFactory WinGetFactory { get; private set; }

    public static PackageManager Manager { get; private set; }

    public static IReadOnlyList<PackageCatalogReference> AvailableCatalogs { get; private set; }

    private static readonly PackageCatalogReference _wingetCatalog;
    private static readonly PackageCatalogReference _storeCatalog;

    public static Lazy<Task<PackageCatalog>> CompositeAllCatalog { get; } = new(() => GetCompositeCatalog(true));

    public static Lazy<Task<PackageCatalog>> CompositeWingetCatalog { get; } = new(() => GetCompositeCatalog(false));

    static WinGetStatics()
    {
        WinGetFactory = new WindowsPackageManagerStandardFactory();

        // Create Package Manager and get available catalogs
        Manager = WinGetFactory.CreatePackageManager();

        _wingetCatalog = Manager.GetPredefinedPackageCatalog(PredefinedPackageCatalog.OpenWindowsCatalog);
        _storeCatalog = Manager.GetPredefinedPackageCatalog(PredefinedPackageCatalog.MicrosoftStore);
        AvailableCatalogs = [
            _wingetCatalog,
            _storeCatalog,
        ];

        foreach (var catalogReference in AvailableCatalogs)
        {
            catalogReference.PackageCatalogBackgroundUpdateInterval = new(0);
        }

        // Immediately start the lazy-init of the all packages catalog, but
        // leave the winget one to be initialized as needed
        _ = Task.Run(() =>
        {
            _ = CompositeAllCatalog.Value;

            // _ = CompositeWingetCatalog.Value;
        });
    }

    internal static async Task<PackageCatalog> GetCompositeCatalog(bool all)
    {
        Stopwatch stopwatch = new();
        Debug.WriteLine($"Starting GetCompositeCatalog({all}) fetch");
        stopwatch.Start();

        // Create the composite catalog
        var createCompositePackageCatalogOptions = WinGetFactory.CreateCreateCompositePackageCatalogOptions();

        if (all)
        {
            // Add winget and the store to this catalog
            foreach (var catalogReference in AvailableCatalogs.ToArray())
            {
                createCompositePackageCatalogOptions.Catalogs.Add(catalogReference);
            }
        }
        else
        {
            createCompositePackageCatalogOptions.Catalogs.Add(_wingetCatalog);
        }

        // Searches only the catalogs provided, but will correlated with installed items
        createCompositePackageCatalogOptions.CompositeSearchBehavior = CompositeSearchBehavior.RemotePackagesFromAllCatalogs;

        var catalogRef = WinGetStatics.Manager.CreateCompositePackageCatalog(createCompositePackageCatalogOptions);

        var connectResult = await catalogRef.ConnectAsync();
        var compositeCatalog = connectResult.PackageCatalog;

        stopwatch.Stop();
        Debug.WriteLine($"GetCompositeCatalog({all}) fetch took {stopwatch.ElapsedMilliseconds}ms");

        return compositeCatalog;
    }
}
