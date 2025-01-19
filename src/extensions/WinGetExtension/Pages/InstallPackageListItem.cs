// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CmdPal.Extensions.Helpers;
using Microsoft.Management.Deployment;

namespace WinGetExtension.Pages;

public partial class InstallPackageListItem : ListItem
{
    private readonly CatalogPackage _package;
    private readonly InstallPackageCommand _installCommand;

    public InstallPackageListItem(CatalogPackage package)
        : base(new InstallPackageCommand(package))
    {
        _package = package;
        _installCommand = (InstallPackageCommand)Command!;

        var version = _package.DefaultInstallVersion;
        var versionText = version.Version;
        var versionTagText = versionText == "Unknown" && version.PackageCatalog.Info.Id == "StoreEdgeFD" ? "msstore" : versionText;

        Title = _package.Name;
        Subtitle = _package.Id;
        Tags = [new Tag() { Text = versionTagText }];

        var metadata = version.GetCatalogPackageMetadata();
        if (metadata != null)
        {
            var detailsBody = $"""
# {metadata.PackageName}
## {metadata.Publisher}

{metadata.Description}
""";
            Details = new Details() { Body = detailsBody };
        }

        _ = Task.Run(UpdatedInstalledStatus);
    }

    private async void UpdatedInstalledStatus()
    {
        var status = await _package.CheckInstalledStatusAsync();
        var isInstalled = _package.InstalledVersion != null;
        Icon = new(isInstalled ? "\uE930" : "\uE896"); // Completed : Download
        _installCommand.Icon = Icon;
        _installCommand.Name = isInstalled ? "Installed" : "Install";

        if (status.Status == CheckInstalledStatusResultStatus.Ok)
        {
            var l = status.PackageInstalledStatus;
            _ = l;
        }
    }
}
