$ErrorActionPreference = 'Stop'
$toolsFolder = Split-Path $PSScriptRoot -Parent
Import-Module (Join-Path $toolsFolder 'MsixPackaging.psm1') -Force
$makeAppx = Get-WindowsSdkTool -Name 'makeappx.exe'
$testRoot = Join-Path ([System.IO.Path]::GetTempPath()) "CmdPal Packaging Tests $([guid]::NewGuid())"
New-Item -ItemType Directory -Path $testRoot | Out-Null

function Assert-Failure {
    param([scriptblock]$Action, [string]$Message)

    try {
        & $Action
    }
    catch {
        if ($_.Exception.Message -notlike "*$Message*") {
            throw "Expected '$Message', got '$($_.Exception.Message)'."
        }
        return
    }
    throw "Expected failure containing '$Message'."
}

function New-TestPackage {
    param(
        [string]$Architecture,
        [string]$Version = '1.2.3.0',
        [string]$Publisher = 'CN=Packaging Tests'
    )

    $content = Join-Path $testRoot ([guid]::NewGuid().ToString())
    New-Item -ItemType Directory -Path $content | Out-Null
    $manifest = @"
<?xml version="1.0" encoding="utf-8"?>
<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
    xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
    xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
    IgnorableNamespaces="uap rescap">
  <Identity Name="PackagingTests" Publisher="$Publisher" Version="$Version" ProcessorArchitecture="$Architecture" />
  <Properties>
    <DisplayName>Packaging Tests</DisplayName>
    <PublisherDisplayName>Packaging Tests</PublisherDisplayName>
    <Logo>Logo.png</Logo>
  </Properties>
  <Dependencies>
    <TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.19041.0" MaxVersionTested="10.0.26100.0" />
  </Dependencies>
  <Resources><Resource Language="en-us" /></Resources>
  <Applications>
    <Application Id="App" Executable="Test.exe" EntryPoint="Windows.FullTrustApplication">
      <uap:VisualElements DisplayName="Packaging Tests" Description="Packaging Tests"
        BackgroundColor="transparent" Square150x150Logo="Logo.png" Square44x44Logo="Logo.png" />
    </Application>
  </Applications>
  <Capabilities><rescap:Capability Name="runFullTrust" /></Capabilities>
</Package>
"@
    Set-Content -LiteralPath (Join-Path $content 'AppxManifest.xml') -Value $manifest -Encoding utf8
    Set-Content -LiteralPath (Join-Path $content 'Test.exe') -Value 'Test payload' -Encoding ascii
    $logo = Join-Path $toolsFolder '..\extensions\HackerNewsExtension\Assets\StoreLogo.png'
    Copy-Item -LiteralPath $logo -Destination (Join-Path $content 'Logo.png')
    $package = Join-Path $testRoot "PackagingTests_${Version}_${Architecture}_$([guid]::NewGuid()).msix"
    & $makeAppx pack /d $content /p $package /nv | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Could not create $Architecture fixture (exit code $LASTEXITCODE)."
    }
    return $package
}

function New-TestInputs {
    param([string]$Name, [string[]]$Packages)

    $folder = Join-Path $testRoot $Name
    New-Item -ItemType Directory -Path $folder | Out-Null
    foreach ($package in $Packages) {
        Copy-Item -LiteralPath $package -Destination $folder
    }
    return $folder
}

function Invoke-TestBundling {
    param([string]$PackagesFolder, [string]$DestinationFolder)

    & (Join-Path $toolsFolder 'New-MsixBundles.ps1') `
        -PackagesFolder $PackagesFolder -DestinationFolder $DestinationFolder
}

try {
    $x64 = New-TestPackage -Architecture x64
    $arm64 = New-TestPackage -Architecture arm64
    $inputs = New-TestInputs -Name valid -Packages $x64, $arm64
    $dependencies = Join-Path $inputs 'Dependencies\arm64'
    New-Item -ItemType Directory -Path $dependencies -Force | Out-Null
    Copy-Item -LiteralPath $arm64 -Destination (Join-Path $dependencies 'Microsoft.WindowsAppRuntime.99.msix')
    $output = Join-Path $testRoot 'bundles'
    Invoke-TestBundling -PackagesFolder $inputs -DestinationFolder $output
    $bundles = @(Get-ChildItem -LiteralPath $output -Filter '*.msixbundle')
    if ($bundles.Count -ne 1 -or $bundles[0].Name -ne 'PackagingTests_1.2.3.0_x64_ARM64.msixbundle') {
        throw 'Expected one versioned dual-architecture bundle, excluding dependency packages.'
    }
    & (Join-Path $toolsFolder 'Test-MsixBundles.ps1') -Path $output
    $bundleManifest = Get-MsixManifest -Path $bundles[0].FullName -EntryName 'AppxMetadata/AppxBundleManifest.xml'
    if ($bundleManifest.Bundle.Identity.Version -ne '1.2.3.0') {
        throw 'The bundle must preserve the package version.'
    }

    Assert-Failure { Invoke-TestBundling $inputs $output } 'already contains bundles'
    Assert-Failure {
        & (Join-Path $toolsFolder 'Test-MsixBundles.ps1') -Path $output -RequireSignature
    } 'unsigned'

    $badSignature = Join-Path $testRoot 'bad-signature.msixbundle'
    Copy-Item -LiteralPath $bundles[0].FullName -Destination $badSignature
    $archive = [System.IO.Compression.ZipFile]::Open($badSignature, [System.IO.Compression.ZipArchiveMode]::Update)
    try {
        $writer = [System.IO.StreamWriter]::new($archive.CreateEntry('AppxSignature.p7x').Open())
        try {
            $writer.Write('Invalid signature')
        }
        finally {
            $writer.Dispose()
        }
    }
    finally {
        $archive.Dispose()
    }
    Assert-Failure { Test-MsixBundle -Path $badSignature -RequireSignature } 'Signature verification failed'

    $missing = New-TestInputs -Name missing -Packages $x64
    Assert-Failure { Invoke-TestBundling $missing (Join-Path $testRoot 'missing-output') } 'exactly one x64 and one ARM64'
    $singleArchitecture = Join-Path $testRoot 'x64-only.msixbundle'
    & $makeAppx bundle /d $missing /p $singleArchitecture /bv '1.2.3.0' | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not create the single-architecture fixture.'
    }
    Assert-Failure { Test-MsixBundle -Path $singleArchitecture } 'exactly one x64 and one ARM64'

    $duplicateX64 = New-TestPackage -Architecture x64
    $duplicate = New-TestInputs -Name duplicate -Packages $x64, $arm64, $duplicateX64
    Assert-Failure { Invoke-TestBundling $duplicate (Join-Path $testRoot 'duplicate-output') } 'exactly one x64 and one ARM64'

    $otherVersion = New-TestPackage -Architecture arm64 -Version '1.2.4.0'
    $mismatch = New-TestInputs -Name mismatch -Packages $x64, $otherVersion
    Assert-Failure { Invoke-TestBundling $mismatch (Join-Path $testRoot 'mismatch-output') } 'versions and publishers must match'

    $otherPublisher = New-TestPackage -Architecture arm64 -Publisher 'CN=Different Publisher'
    $publisher = New-TestInputs -Name publisher -Packages $x64, $otherPublisher
    Assert-Failure { Invoke-TestBundling $publisher (Join-Path $testRoot 'publisher-output') } 'versions and publishers must match'

    $x86 = New-TestPackage -Architecture x86
    $unsupported = New-TestInputs -Name unsupported -Packages $x64, $x86
    Assert-Failure { Invoke-TestBundling $unsupported (Join-Path $testRoot 'unsupported-output') } 'exactly one x64 and one ARM64'

    $empty = New-TestInputs -Name empty -Packages @()
    Assert-Failure { Invoke-TestBundling $empty (Join-Path $testRoot 'empty-output') } 'No application MSIX packages'
    Assert-Failure {
        & (Join-Path $toolsFolder 'Test-MsixBundles.ps1') -Path $empty
    } 'No MSIX bundles'

    $archive = [System.IO.Compression.ZipFile]::Open($bundles[0].FullName, [System.IO.Compression.ZipArchiveMode]::Update)
    try {
        $archive.GetEntry($bundleManifest.Bundle.Packages.Package[0].FileName).Delete()
    }
    finally {
        $archive.Dispose()
    }
    Assert-Failure { Test-MsixBundle -Path $bundles[0].FullName } 'missing payload'

    Write-Host 'All MSIX packaging tests passed.'
}
finally {
    Remove-Item -LiteralPath $testRoot -Recurse -Force
}
