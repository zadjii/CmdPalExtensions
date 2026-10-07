param([string]$BundlesPath)

$ErrorActionPreference = 'Stop'
$tools = Split-Path $PSScriptRoot -Parent
$root = (Resolve-Path (Join-Path $tools '..\..')).Path
$validator = Join-Path $tools 'Test-StoreReadiness.ps1'
& $validator

function Assert-Failure {
    param([scriptblock]$Action, [string]$Expected)
    try { & $Action }
    catch {
        if ($_.Exception.Message -notlike "*$Expected*") { throw }
        return
    }
    throw "Expected failure containing: $Expected"
}

$fixture = Join-Path ([System.IO.Path]::GetTempPath()) "CmdPalStoreTests-$([guid]::NewGuid())"
try {
    New-Item -ItemType Directory -Path (Join-Path $fixture 'src'), (Join-Path $fixture 'doc\store\assets'), (Join-Path $fixture 'doc\store\listings') -Force | Out-Null
    $catalog = Get-Content (Join-Path $root 'src\store-apps.json') -Raw | ConvertFrom-Json
    $app = $catalog.apps | Where-Object { $_.id -eq 'obsidian' }
    $catalog.apps = @($app)
    $catalogPath = Join-Path $fixture 'src\store-apps.json'
    $catalog | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $catalogPath
    foreach ($name in 'LICENSE', 'PRIVACY.md', 'THIRD-PARTY-NOTICES.md') {
        Copy-Item -LiteralPath (Join-Path $root $name) -Destination $fixture
    }
    Copy-Item -LiteralPath (Join-Path $root 'doc\licenses') -Destination (Join-Path $fixture 'doc') -Recurse
    $directory = Split-Path $app.project -Parent
    New-Item -ItemType Directory -Path (Join-Path $fixture $directory) -Force | Out-Null
    foreach ($name in 'Package.appxmanifest', 'Program.cs', (Split-Path $app.project -Leaf)) {
        Copy-Item -LiteralPath (Join-Path $root "$directory\$name") -Destination (Join-Path $fixture $directory)
    }
    Copy-Item -LiteralPath (Join-Path $root "$directory\Assets") -Destination (Join-Path $fixture $directory) -Recurse
    Copy-Item -LiteralPath (Join-Path $root "doc\store\assets\$($app.id)-store-logo.png") -Destination (Join-Path $fixture 'doc\store\assets')
    Copy-Item -LiteralPath (Join-Path $root "doc\store\listings\$($app.id).md") -Destination (Join-Path $fixture 'doc\store\listings')
    & $validator -RepositoryRoot $fixture

    $sourceIcon = Join-Path $fixture "$directory\$($app.iconSource)"
    Remove-Item -LiteralPath $sourceIcon
    Assert-Failure { & $validator -RepositoryRoot $fixture } 'Original icon is missing'
    Copy-Item -LiteralPath (Join-Path $root "$directory\$($app.iconSource)") -Destination $sourceIcon
    $projectPath = Join-Path $fixture $app.project
    $projectText = Get-Content -LiteralPath $projectPath -Raw
    Set-Content -LiteralPath $projectPath -Value $projectText.Replace('<Content Include="' + $app.iconSource + '" />', '')
    Assert-Failure { & $validator -RepositoryRoot $fixture } 'Original icon is missing'
    Set-Content -LiteralPath $projectPath -Value $projectText

    $path = Join-Path $fixture "$directory\Package.appxmanifest"
    $original = Get-Content -LiteralPath $path -Raw
    Set-Content -LiteralPath $path -Value $original.Replace($catalog.publisher, 'CN=Wrong')
    Assert-Failure { & $validator -RepositoryRoot $fixture } 'Package identity mismatch'
    Set-Content -LiteralPath $path -Value $original

    $catalog.apps = @($app, $app)
    $catalog | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $catalogPath
    Assert-Failure { & $validator -RepositoryRoot $fixture } 'unique'
    $catalog.apps = @($app)
    $catalog | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $catalogPath

    $icon = Join-Path $fixture "$directory\Assets\Package\StoreLogo.scale-100.png"
    Copy-Item -LiteralPath (Join-Path $fixture "$directory\Assets\Package\StoreLogo.scale-200.png") -Destination $icon -Force
    Assert-Failure { & $validator -RepositoryRoot $fixture } 'Incorrect dimensions'
    Copy-Item -LiteralPath (Join-Path $root "$directory\Assets\Package\StoreLogo.scale-100.png") -Destination $icon -Force

    $program = Join-Path $fixture "$directory\Program.cs"
    Set-Content -LiteralPath $program -Value 'No standalone instructions'
    Assert-Failure { & $validator -RepositoryRoot $fixture } 'standalone launch guidance'
    Copy-Item -LiteralPath (Join-Path $root "$directory\Program.cs") -Destination $program -Force

    $bundles = Join-Path $fixture 'bundles'
    New-Item -ItemType Directory -Path $bundles | Out-Null
    Assert-Failure { & $validator -RepositoryRoot $fixture -BundlesPath $bundles } 'exactly the apps'
    New-Item -ItemType File -Path (Join-Path $bundles 'Unexpected.msixbundle') | Out-Null
    Assert-Failure { & $validator -RepositoryRoot $fixture -BundlesPath $bundles } 'Missing expected bundle'
    Remove-Item -LiteralPath (Join-Path $bundles 'Unexpected.msixbundle')

    if ($BundlesPath) {
        $manifest = [xml]$original
        $bundleName = "$($app.identityName)_$($manifest.Package.Identity.Version)_x64_ARM64.msixbundle"
        $bundle = Join-Path $bundles $bundleName
        Copy-Item -LiteralPath (Join-Path $BundlesPath $bundleName) -Destination $bundle
        & $validator -RepositoryRoot $fixture -BundlesPath $bundles
        Copy-Item -LiteralPath (Join-Path $root 'src\extensions\HackerNewsExtension\Assets\Package\StoreLogo.scale-100.png') -Destination $icon -Force
        Assert-Failure { & $validator -RepositoryRoot $fixture -BundlesPath $bundles } 'Stale packaged artwork'
        Copy-Item -LiteralPath (Join-Path $root "$directory\Assets\Package\StoreLogo.scale-100.png") -Destination $icon -Force
        [System.IO.File]::AppendAllText($sourceIcon, 'changed')
        Assert-Failure { & $validator -RepositoryRoot $fixture -BundlesPath $bundles } 'Modified packaged original icon'
        Copy-Item -LiteralPath (Join-Path $root "$directory\$($app.iconSource)") -Destination $sourceIcon -Force
        $archive = [System.IO.Compression.ZipFile]::Open($bundle, [System.IO.Compression.ZipArchiveMode]::Update)
        try {
            $entry = $archive.Entries | Where-Object { $_.FullName.EndsWith('.msix') } | Select-Object -First 1
            $innerPath = Join-Path $fixture 'payload.msix'
            [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $innerPath)
            $name = $entry.FullName
            $entry.Delete()
            $inner = [System.IO.Compression.ZipFile]::Open($innerPath, [System.IO.Compression.ZipArchiveMode]::Update)
            try { $inner.GetEntry('Assets/Package/StoreLogo.scale-100.png').Delete() }
            finally { $inner.Dispose() }
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $innerPath, $name) | Out-Null
        }
        finally { $archive.Dispose() }
        Assert-Failure { & $validator -RepositoryRoot $fixture -BundlesPath $bundles } 'Missing packaged icon'
    }

    Remove-Item -LiteralPath (Join-Path $fixture 'PRIVACY.md')
    Assert-Failure { & $validator -RepositoryRoot $fixture } 'Missing legal document'
    Write-Host 'Store readiness regressions passed.'
}
finally {
    if (Test-Path -LiteralPath $fixture) { Remove-Item -LiteralPath $fixture -Recurse -Force }
}
