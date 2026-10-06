param(
    [string]$PackagesFolder = (Join-Path $PSScriptRoot '..\..\artifacts\packages'),
    [string]$DestinationFolder = (Join-Path $PSScriptRoot '..\..\artifacts\bundles')
)

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'MsixPackaging.psm1') -Force

$packages = @(Get-ChildItem -LiteralPath $PackagesFolder -Recurse -File -Filter '*.msix' |
    Where-Object { $_.FullName -notmatch '[\\/]Dependencies[\\/]' } |
    ForEach-Object {
        $manifest = Get-MsixManifest -Path $_.FullName
        $identity = $manifest.Package.Identity
        if (-not $identity -or -not $manifest.Package.Applications.Application) {
            throw "Not an application package: $($_.FullName)."
        }

        [pscustomobject]@{
            Path = $_.FullName
            FileName = $_.Name
            Name = $identity.Name
            Version = $identity.Version
            Publisher = $identity.Publisher
            Architecture = $identity.ProcessorArchitecture.ToLowerInvariant()
        }
    })
if ($packages.Count -eq 0) {
    throw "No application MSIX packages found in $PackagesFolder."
}

$groups = @($packages | Group-Object Name)
foreach ($group in $groups) {
    $architectures = @($group.Group.Architecture | Sort-Object)
    if ($group.Count -ne 2 -or ($architectures -join ',') -ne 'arm64,x64') {
        throw "$($group.Name) must have exactly one x64 and one ARM64 package; found $($architectures -join ', ')."
    }

    if (@($group.Group.Version | Select-Object -Unique).Count -ne 1 -or
        @($group.Group.Publisher | Select-Object -Unique).Count -ne 1) {
        throw "Package versions and publishers must match for $($group.Name)."
    }
}

if (Test-Path -LiteralPath $DestinationFolder) {
    if (Get-ChildItem -LiteralPath $DestinationFolder -Filter '*.msixbundle' -File) {
        throw "The bundle destination already contains bundles: $DestinationFolder. Use an empty output folder."
    }
}
else {
    New-Item -ItemType Directory -Path $DestinationFolder -Force | Out-Null
}
$DestinationFolder = (Resolve-Path -LiteralPath $DestinationFolder).Path
$makeAppx = Get-WindowsSdkTool -Name 'makeappx.exe'

foreach ($group in $groups) {
    $stagingFolder = Join-Path ([System.IO.Path]::GetTempPath()) "CmdPalBundle-$([guid]::NewGuid())"
    New-Item -ItemType Directory -Path $stagingFolder | Out-Null
    try {
        foreach ($package in $group.Group) {
            Copy-Item -LiteralPath $package.Path -Destination (Join-Path $stagingFolder $package.FileName)
        }

        $version = $group.Group[0].Version
        $bundle = Join-Path $DestinationFolder "$($group.Name)_${version}_x64_ARM64.msixbundle"
        & $makeAppx bundle /d $stagingFolder /p $bundle /bv $version
        if ($LASTEXITCODE -ne 0) {
            throw "Bundling failed for $($group.Name) (exit code $LASTEXITCODE)."
        }

        Test-MsixBundle -Path $bundle
        Write-Host "Created $bundle"
    }
    finally {
        Remove-Item -LiteralPath $stagingFolder -Recurse -Force
    }
}
