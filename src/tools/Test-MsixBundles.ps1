param(
    [string]$Path = (Join-Path $PSScriptRoot '..\..\artifacts\bundles'),
    [switch]$RequireSignature
)

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'MsixPackaging.psm1') -Force

$bundles = @(Get-ChildItem -LiteralPath $Path -File -Filter '*.msixbundle')
if ($bundles.Count -eq 0) {
    throw "No MSIX bundles found in $Path."
}

foreach ($bundle in $bundles) {
    Test-MsixBundle -Path $bundle.FullName -RequireSignature:$RequireSignature
    Write-Host "Verified $($bundle.Name)"
}
