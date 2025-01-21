// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CmdPal.Extensions;
using Microsoft.CmdPal.Extensions.Helpers;
using Microsoft.Management.Deployment;
using Windows.Foundation;

namespace WinGetExtension.Pages;

public partial class InstallPackageCommand : InvokableCommand
{
    private readonly CatalogPackage _package;

    private readonly StatusMessage _installBanner = new();
    private IAsyncOperationWithProgress<InstallResult, InstallProgress>? _installAction;

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

                var installOptions = WinGetStatics.WinGetFactory.CreateInstallOptions();
                installOptions.PackageInstallScope = PackageInstallScope.Any;

                _installAction = WinGetStatics.Manager.InstallPackageAsync(_package, installOptions);
                var handler = new AsyncOperationProgressHandler<InstallResult, InstallProgress>(OnInstallProgress);
                _installAction.Progress = handler;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _installAction.AsTask();
                    }
                    catch (Exception ex)
                    {
                        _installBanner.State = MessageState.Error;
                        _installBanner.Message = ex.Message;
                    }
                });
            }
        }

        return CommandResult.KeepOpen();
    }

    private void OnInstallProgress(
        IAsyncOperationWithProgress<InstallResult, InstallProgress> operation,
        InstallProgress progress)
    {
        var downloadText = "Downloading. ";
        switch (progress.State)
        {
            case PackageInstallProgressState.Queued:
                _installBanner.Message = "Queued";
                break;
            case PackageInstallProgressState.Downloading:
                downloadText += $"{progress.BytesDownloaded} bytes of {progress.BytesRequired}";
                _installBanner.Message = downloadText;
                break;
            case PackageInstallProgressState.Installing:
                _installBanner.Message = "Installing";
                _installBanner.Progress = new ProgressState() { IsIndeterminate = true };
                break;
            case PackageInstallProgressState.PostInstall:
                _installBanner.Message = "Finishing install";
                break;
            case PackageInstallProgressState.Finished:
                _installBanner.Message = "Finished install.";

                // progressBar.IsIndeterminate(false);
                _installBanner.Progress = null;
                _installBanner.State = MessageState.Success;
                break;
            default:
                _installBanner.Message = string.Empty;
        }
    }
}
