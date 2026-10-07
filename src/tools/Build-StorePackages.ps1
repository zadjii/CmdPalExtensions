param(
    [Parameter(Mandatory)]
    [ValidateSet('x64', 'ARM64')]
    [string]$Platform,
    [string]$OutputPath = (Join-Path $PSScriptRoot '..\..\artifacts\packages')
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$catalog = Get-Content (Join-Path $root 'src\store-apps.json') -Raw | ConvertFrom-Json
& (Join-Path $PSScriptRoot 'Test-StoreReadiness.ps1')
$destination = Join-Path ([System.IO.Path]::GetFullPath($OutputPath)) $Platform
if ((Test-Path -LiteralPath $destination) -and
    (Get-ChildItem -LiteralPath $destination -Recurse -File)) {
    throw "Use an empty package output directory: $destination"
}

foreach ($app in $catalog.apps) {
    $project = Join-Path $root $app.project
    $packages = (Join-Path $destination $app.id) + '\'
    & dotnet restore $project --configfile (Join-Path $root '.github\nuget.config') "-p:Platform=$Platform"
    if ($LASTEXITCODE -ne 0) {
        throw "Restore failed for $($app.id) / $Platform (exit code $LASTEXITCODE)."
    }
    & dotnet build $project --configuration Release --no-restore "-p:Platform=$Platform" `
        "-p:SolutionDir=$root\" "-p:AppxPackageDir=$packages" -p:PublishProfile= `
        -p:AppxBundle=Never -p:AppxPackageSigningEnabled=false
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed for $($app.id) / $Platform (exit code $LASTEXITCODE)."
    }
}
