$ErrorActionPreference = 'Stop'

function Get-WindowsSdkTool {
    param(
        [Parameter(Mandatory)]
        [ValidateSet('makeappx.exe', 'signtool.exe')]
        [string]$Name
    )

    $sdkRoot = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\bin'
    $tool = Get-ChildItem -LiteralPath $sdkRoot -Directory |
        Where-Object { $_.Name -match '^\d+\.\d+\.\d+\.\d+$' } |
        Sort-Object { [version]$_.Name } -Descending |
        ForEach-Object { Join-Path $_.FullName "x64\$Name" } |
        Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
        Select-Object -First 1
    if (-not $tool) {
        throw "Cannot find $Name. Install the Windows 10 or 11 SDK."
    }

    return $tool
}

function Get-MsixManifest {
    param(
        [Parameter(Mandatory)]
        [string]$Path,

        [string]$EntryName = 'AppxManifest.xml'
    )

    $archive = [System.IO.Compression.ZipFile]::OpenRead($Path)
    try {
        $entry = $archive.GetEntry($EntryName)
        if (-not $entry) {
            throw "Missing $EntryName in $Path."
        }

        $reader = [System.IO.StreamReader]::new($entry.Open())
        try {
            return [xml]$reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }
    }
    finally {
        $archive.Dispose()
    }
}

function Test-MsixBundle {
    param(
        [Parameter(Mandatory)]
        [string]$Path,

        [switch]$RequireSignature
    )

    $manifest = Get-MsixManifest -Path $Path -EntryName 'AppxMetadata/AppxBundleManifest.xml'
    $packages = @($manifest.Bundle.Packages.Package | Where-Object { $_.Type -eq 'application' })
    $architectures = @($packages | ForEach-Object { $_.Architecture.ToLowerInvariant() } | Sort-Object)
    if ($packages.Count -ne 2 -or ($architectures -join ',') -ne 'arm64,x64') {
        throw "Bundle $Path must contain exactly one x64 and one ARM64 application package."
    }

    $archive = [System.IO.Compression.ZipFile]::OpenRead($Path)
    try {
        foreach ($package in $packages) {
            if (-not $archive.GetEntry($package.FileName)) {
                throw "Bundle $Path is missing payload $($package.FileName)."
            }
        }

        if ($RequireSignature -and -not $archive.GetEntry('AppxSignature.p7x')) {
            throw "Bundle $Path is unsigned."
        }
    }
    finally {
        $archive.Dispose()
    }

    if ($RequireSignature) {
        $signTool = Get-WindowsSdkTool -Name 'signtool.exe'
        & $signTool verify /pa /all $Path
        if ($LASTEXITCODE -ne 0) {
            throw "Signature verification failed for $Path (exit code $LASTEXITCODE)."
        }
    }
}

Export-ModuleMember -Function Get-WindowsSdkTool, Get-MsixManifest, Test-MsixBundle
