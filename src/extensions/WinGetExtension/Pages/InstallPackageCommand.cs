// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CmdPal.Extensions;
using Microsoft.CmdPal.Extensions.Helpers;
using Microsoft.Management.Deployment;

namespace WinGetExtension.Pages;

public partial class InstallPackageCommand : InvokableCommand
{
    private readonly CatalogPackage _package;

    private readonly StatusMessage _installBanner = new();

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
            var isInstalled = _package.InstalledVersion != null;

            if (isInstalled)
            {
                _installBanner.State = MessageState.Info;
                _installBanner.Message = $"{_package.Name} is already installed";
                ExtensionHost.ShowStatus(_installBanner);

                // TODO Derp, didn't expose HideStatus from API
                // _ = Task.Run(() =>
                // {
                //    Thread.Sleep(2000);
                //    ExtensionHost.HideStatus(_installBanner);
                // });
            }
            else
            {
                _installBanner.State = MessageState.Info;
                _installBanner.Message = $"Installing {_package.Name}...";
                ExtensionHost.ShowStatus(_installBanner);
                _ = Task.Run(() =>
                {
                    Thread.Sleep(2000);
                    _installBanner.State = MessageState.Success;
                    _installBanner.Message = $"Successfully installed {_package.Name}";
                });
            }
        }

        return CommandResult.KeepOpen();
    }
}
