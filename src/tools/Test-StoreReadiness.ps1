param(
    [string]$RepositoryRoot = (Join-Path $PSScriptRoot '..\..'),
    [string]$BundlesPath
)

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'MsixPackaging.psm1') -Force
$root = (Resolve-Path -LiteralPath $RepositoryRoot).Path
$catalog = Get-Content (Join-Path $root 'src\store-apps.json') -Raw | ConvertFrom-Json
if ($catalog.publisher -notmatch '^CN=[0-9A-Fa-f-]{36}$' -or -not $catalog.publisherDisplayName) {
    throw 'A Store publisher identity and display name are required.'
}
$apps = @($catalog.apps)
if ($apps.Count -eq 0 -or
    @($apps.id | Select-Object -Unique).Count -ne $apps.Count -or
    @($apps.identityName | Select-Object -Unique).Count -ne $apps.Count) {
    throw 'Store app IDs and package identities must be nonempty and unique.'
}

function Assert-Png {
    param([byte[]]$Bytes, [int]$Width, [int]$Height, [string]$Name)

    if ($Bytes.Length -lt 24 -or [BitConverter]::ToString($Bytes[0..7]) -ne '89-50-4E-47-0D-0A-1A-0A') {
        throw "Invalid PNG: $Name"
    }
    $actualWidth = [System.Net.IPAddress]::NetworkToHostOrder([BitConverter]::ToInt32($Bytes, 16))
    $actualHeight = [System.Net.IPAddress]::NetworkToHostOrder([BitConverter]::ToInt32($Bytes, 20))
    if ($actualWidth -ne $Width -or $actualHeight -ne $Height) {
        throw "Incorrect dimensions for ${Name}: ${actualWidth}x${actualHeight}, expected ${Width}x${Height}."
    }
}

function Assert-Manifest {
    param([xml]$Manifest, $App, [switch]$Built)

    $identity = $Manifest.Package.Identity
    if ($identity.Name -ne $App.identityName -or $identity.Publisher -ne $catalog.publisher) {
        throw "Package identity mismatch for $($App.id)."
    }
    if ($identity.Name.Length -gt 50 -or $identity.Name -notmatch '^zadjii\.[A-Za-z0-9.]+$') {
        throw "Invalid Store package name for $($App.id)."
    }
    $version = [version]$identity.Version
    if ($version.Revision -ne 0 -or $version.Major -gt 65535 -or $version.Minor -gt 65535 -or $version.Build -gt 65535) {
        throw "Store package versions must be four components ending in .0: $($App.id)."
    }
    if ($Manifest.Package.Properties.DisplayName -ne $App.displayName -or
        $Manifest.Package.Properties.PublisherDisplayName -ne $catalog.publisherDisplayName) {
        throw "Display names do not match the Store catalog for $($App.id)."
    }
    $families = @($Manifest.Package.Dependencies.TargetDeviceFamily)
    if ($families.Count -ne 1 -or $families[0].Name -ne 'Windows.Desktop') {
        throw "Store apps must target Windows desktop only: $($App.id)."
    }
    $classes = @($Manifest.SelectNodes("//*[local-name()='Class']"))
    $activation = @($Manifest.SelectNodes("//*[local-name()='CreateInstance']"))
    $parsed = [guid]::Empty
    if ($classes.Count -ne 1 -or $activation.Count -ne 1 -or
        -not [guid]::TryParse($classes[0].Id, [ref]$parsed) -or
        $classes[0].Id -ne $activation[0].ClassId -or
        $parsed -eq [guid]'ffffffff-ffff-ffff-ffff-ffffffffffff') {
        throw "Invalid CmdPal COM activation for $($App.id)."
    }
    $extension = $Manifest.SelectSingleNode("//*[local-name()='AppExtension']")
    if ($extension.Name -ne 'com.microsoft.commandpalette') {
        throw "Missing Command Palette extension registration for $($App.id)."
    }
    $capabilities = @($Manifest.Package.Capabilities.ChildNodes | ForEach-Object { $_.GetAttribute('Name') })
    if ('runFullTrust' -notin $capabilities -or ($App.id -eq 'media' -and 'globalMediaControl' -notin $capabilities)) {
        throw "Missing required capabilities for $($App.id)."
    }
    if ($Built -and $identity.ProcessorArchitecture -notin 'x64', 'arm64') {
        throw "Unexpected application architecture for $($App.id)."
    }
}

$legalFiles = @('LICENSE', 'PRIVACY.md', 'THIRD-PARTY-NOTICES.md')
foreach ($file in $legalFiles) {
    if (-not (Test-Path -LiteralPath (Join-Path $root $file) -PathType Leaf)) {
        throw "Missing legal document: $file"
    }
}
$licenses = @(Get-ChildItem -LiteralPath (Join-Path $root 'doc\licenses') -File -Filter '*.txt')
if ($licenses.Count -lt 7) { throw 'Missing third-party license texts.' }
if ($BundlesPath) {
    if (@(Get-ChildItem -LiteralPath $BundlesPath -File -Filter '*.msixbundle').Count -ne $apps.Count) {
        throw 'The bundle set must contain exactly the apps in the Store catalog, without the template or stale packages.'
    }
    foreach ($app in $apps) {
        $source = [xml](Get-Content (Join-Path (Split-Path (Join-Path $root $app.project)) 'Package.appxmanifest') -Raw)
        $expected = "$($app.identityName)_$($source.Package.Identity.Version)_x64_ARM64.msixbundle"
        if (-not (Test-Path -LiteralPath (Join-Path $BundlesPath $expected))) {
            throw "Missing expected bundle: $expected"
        }
    }
}
$expectedAssets = @{}
foreach ($size in @(
    @{ Name = 'Square44x44Logo'; Width = 44; Height = 44 },
    @{ Name = 'Square150x150Logo'; Width = 150; Height = 150 },
    @{ Name = 'Wide310x150Logo'; Width = 310; Height = 150 },
    @{ Name = 'SplashScreen'; Width = 620; Height = 300 },
    @{ Name = 'StoreLogo'; Width = 50; Height = 50 }
)) {
    foreach ($scale in 100, 200, 400) {
        $expectedAssets["$($size.Name).scale-$scale.png"] = @(($size.Width * $scale / 100), ($size.Height * $scale / 100))
    }
}
foreach ($size in 16, 24, 32, 48, 256) {
    foreach ($form in 'unplated', 'lightunplated') {
        $expectedAssets["Square44x44Logo.targetsize-${size}_altform-$form.png"] = @($size, $size)
    }
}

foreach ($app in $apps) {
    $project = Join-Path $root $app.project
    $directory = Split-Path $project -Parent
    $source = [xml](Get-Content (Join-Path $directory 'Package.appxmanifest') -Raw)
    Assert-Manifest $source $app
    $projectText = Get-Content -LiteralPath $project -Raw
    if (-not $projectText.Contains('Content Include="Assets\Package\*.png"')) {
        throw "Package icons are not included as Content for $($app.id)."
    }
    if ($app.iconSource) {
        if (-not (Test-Path -LiteralPath (Join-Path $directory $app.iconSource) -PathType Leaf) -or
            -not $projectText.Contains('Content Include="' + $app.iconSource + '"')) {
            throw "Original icon is missing or not included as Content for $($app.id)."
        }
    }
    $program = Get-Content (Join-Path $directory 'Program.cs') -Raw
    if (-not $program.Contains('ExtensionLaunch.ShowHelp("' + $app.displayName + '")')) {
        throw "Missing standalone launch guidance for $($app.id)."
    }
    $assetReferences = @(
        $source.Package.Properties.Logo,
        $source.Package.Applications.Application.VisualElements.Square150x150Logo,
        $source.Package.Applications.Application.VisualElements.Square44x44Logo,
        $source.Package.Applications.Application.VisualElements.DefaultTile.Wide310x150Logo,
        $source.Package.Applications.Application.VisualElements.SplashScreen.Image
    )
    foreach ($reference in $assetReferences) {
        if (-not $reference -or -not $reference.StartsWith('Assets\Package\') -or
            -not (Test-Path -LiteralPath (Join-Path $directory ($reference -replace '\.png$', '.scale-100.png')))) {
            throw "Unresolved manifest image for $($app.id): $reference"
        }
    }
    foreach ($name in $expectedAssets.Keys) {
        $path = Join-Path $directory "Assets\Package\$name"
        Assert-Png ([System.IO.File]::ReadAllBytes($path)) $expectedAssets[$name][0] $expectedAssets[$name][1] $path
    }
    $logo = Join-Path $root "doc\store\assets\$($app.id)-store-logo.png"
    Assert-Png ([System.IO.File]::ReadAllBytes($logo)) 300 300 $logo
    if (-not (Test-Path -LiteralPath (Join-Path $root "doc\store\listings\$($app.id).md"))) {
        throw "Missing Store listing draft for $($app.id)."
    }

    if ($BundlesPath) {
        $bundle = Join-Path $BundlesPath "$($app.identityName)_$($source.Package.Identity.Version)_x64_ARM64.msixbundle"
        Test-MsixBundle -Path ([System.IO.Path]::GetFullPath($bundle))
        $manifest = Get-MsixManifest -Path $bundle -EntryName 'AppxMetadata/AppxBundleManifest.xml'
        if ($manifest.Bundle.Identity.Name -ne $app.identityName -or
            $manifest.Bundle.Identity.Publisher -ne $catalog.publisher -or
            $manifest.Bundle.Identity.Version -ne $source.Package.Identity.Version) {
            throw "Bundle identity mismatch for $($app.id)."
        }
        $archive = [System.IO.Compression.ZipFile]::OpenRead([System.IO.Path]::GetFullPath($bundle))
        try {
            if ($archive.GetEntry('AppxSignature.p7x')) {
                throw "Store submission bundles must not use the legacy signing path: $bundle"
            }
            foreach ($package in $manifest.Bundle.Packages.Package | Where-Object { $_.Type -eq 'application' }) {
                $temporary = Join-Path ([System.IO.Path]::GetTempPath()) "$([guid]::NewGuid()).msix"
                try {
                    [System.IO.Compression.ZipFileExtensions]::ExtractToFile($archive.GetEntry($package.FileName), $temporary)
                    $innerManifest = Get-MsixManifest -Path $temporary
                    Assert-Manifest $innerManifest $app -Built
                    if ($innerManifest.Package.Identity.Version -ne $source.Package.Identity.Version -or
                        $innerManifest.Package.Identity.ProcessorArchitecture -ne $package.Architecture) {
                        throw "Embedded package version/architecture mismatch for $($app.id)."
                    }
                    $inner = [System.IO.Compression.ZipFile]::OpenRead($temporary)
                    try {
                        foreach ($file in $legalFiles + @($innerManifest.Package.Applications.Application.Executable, 'resources.pri', 'coreclr.dll', 'System.Private.CoreLib.dll')) {
                            if (-not $inner.GetEntry($file)) { throw "Missing packaged file ${file}: $($app.id)." }
                        }
                        foreach ($license in $licenses) {
                            if (-not $inner.GetEntry("ThirdPartyLicenses/$($license.Name)")) {
                                throw "Missing packaged license $($license.Name): $($app.id)."
                            }
                        }
                        foreach ($name in $expectedAssets.Keys) {
                            $entry = $inner.GetEntry("Assets/Package/$name")
                            if (-not $entry) { throw "Missing packaged icon ${name}: $($app.id)." }
                            $stream = $entry.Open()
                            $memory = [System.IO.MemoryStream]::new()
                            try {
                                $stream.CopyTo($memory)
                                Assert-Png $memory.ToArray() $expectedAssets[$name][0] $expectedAssets[$name][1] $name
                                $expectedBytes = [System.IO.File]::ReadAllBytes((Join-Path $directory "Assets\Package\$name"))
                                if ([Convert]::ToBase64String($memory.ToArray()) -cne [Convert]::ToBase64String($expectedBytes)) {
                                    throw "Stale packaged artwork ${name}: $($app.id)."
                                }
                            }
                            finally { $stream.Dispose(); $memory.Dispose() }
                        }
                        if ($app.id -eq 'icons' -and -not $inner.GetEntry('Assets/icons.json')) {
                            throw 'The Segoe Icons package is missing its icon data.'
                        }
                        if ($app.iconSource) {
                            $entry = $inner.GetEntry($app.iconSource.Replace('\', '/'))
                            if (-not $entry) { throw "Missing packaged original icon: $($app.id)." }
                            $stream = $entry.Open()
                            $memory = [System.IO.MemoryStream]::new()
                            try {
                                $stream.CopyTo($memory)
                                $expectedBytes = [System.IO.File]::ReadAllBytes((Join-Path $directory $app.iconSource))
                                if ([Convert]::ToBase64String($memory.ToArray()) -cne [Convert]::ToBase64String($expectedBytes)) {
                                    throw "Modified packaged original icon: $($app.id)."
                                }
                            }
                            finally { $stream.Dispose(); $memory.Dispose() }
                        }
                    }
                    finally { $inner.Dispose() }
                }
                finally {
                    if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Force }
                }
            }
        }
        finally { $archive.Dispose() }
    }
}

Write-Host "Verified Store preparation for $($apps.Count) separate apps. Partner Center reservation and certification remain external gates."
