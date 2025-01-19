// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Management.Deployment;
using WindowsPackageManager.Interop;

namespace WinGetExtension.Pages;

internal static class WinGetStatics
{
    public static WindowsPackageManagerStandardFactory WinGetFactory { get; private set; }

    public static PackageManager Manager { get; private set; }

    public static IReadOnlyList<PackageCatalogReference> AvailableCatalogs { get; private set; }

    public static IEnumerable<PackageCatalog> Connections { get; private set; }

    private static PackageCatalog? _compositeCatalog;

    static WinGetStatics()
    {
        WinGetFactory = new WindowsPackageManagerStandardFactory();

        // Create Package Manager and get available catalogs
        Manager = WinGetFactory.CreatePackageManager();

        AvailableCatalogs = [
            Manager.GetPredefinedPackageCatalog(PredefinedPackageCatalog.OpenWindowsCatalog),
            Manager.GetPredefinedPackageCatalog(PredefinedPackageCatalog.MicrosoftStore),
        ];

        foreach (var catalogReference in AvailableCatalogs)
        {
            catalogReference.PackageCatalogBackgroundUpdateInterval = new(0);
        }

        Connections = AvailableCatalogs
            .ToArray()
            .Select(reference => reference.Connect().PackageCatalog);
    }

    internal static async Task<PackageCatalog> GetCompositeCatalog()
    {
        if (_compositeCatalog != null)
        {
            return _compositeCatalog;
        }

        Stopwatch stopwatch = new();
        Debug.WriteLine("Starting GetCompositeCatalog fetch");
        stopwatch.Start();

        // Create the composite catalog
        var createCompositePackageCatalogOptions = WinGetFactory.CreateCreateCompositePackageCatalogOptions();

        // Add winget and the store to this catalog
        foreach (var catalogReference in WinGetStatics.AvailableCatalogs.ToArray())
        {
            createCompositePackageCatalogOptions.Catalogs.Add(catalogReference);
        }

        // Searches only the catalogs provided, but will correlated with installed items
        createCompositePackageCatalogOptions.CompositeSearchBehavior = CompositeSearchBehavior.RemotePackagesFromAllCatalogs;

        var catalogRef = WinGetStatics.Manager.CreateCompositePackageCatalog(createCompositePackageCatalogOptions);

        var connectResult = await catalogRef.ConnectAsync();
        var compositeCatalog = connectResult.PackageCatalog;
        _compositeCatalog = compositeCatalog;

        stopwatch.Stop();
        Debug.WriteLine($"GetCompositeCatalog fetch took {stopwatch.ElapsedMilliseconds}ms");

        return compositeCatalog;
    }
}
