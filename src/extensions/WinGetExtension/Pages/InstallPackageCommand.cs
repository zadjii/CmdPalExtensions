// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CmdPal.Extensions;
using Microsoft.CmdPal.Extensions.Helpers;
using Microsoft.Management.Deployment;

namespace WinGetExtension.Pages;

public partial class InstallPackageCommand : InvokableCommand
{
    private readonly CatalogPackage _package;

    public InstallPackageCommand(CatalogPackage package)
    {
        _package = package;
        Name = "Install";
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
