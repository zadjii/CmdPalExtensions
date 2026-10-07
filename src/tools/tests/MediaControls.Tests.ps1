$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$fixture = Join-Path ([System.IO.Path]::GetTempPath()) "CmdPalMediaTests-$([guid]::NewGuid())"
try {
    New-Item -ItemType Directory -Path $fixture | Out-Null
    # Isolate these API-fake tests from the app's MSIX and WinRT build targets.
    @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
'@ | Set-Content -LiteralPath (Join-Path $fixture 'MediaTests.csproj')
    Copy-Item -LiteralPath (Join-Path $root 'src\extensions\MediaControlsExtension\MediaListItem.cs') -Destination $fixture
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'MediaControls.TestHost.cs') -Destination $fixture
    & dotnet restore (Join-Path $fixture 'MediaTests.csproj') --configfile (Join-Path $root '.github\nuget.config') --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw 'Media test host restore failed.' }
    & dotnet run --project (Join-Path $fixture 'MediaTests.csproj') --no-restore --configuration Release --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw 'Media session regression tests failed.' }
}
finally {
    if (Test-Path -LiteralPath $fixture) { Remove-Item -LiteralPath $fixture -Recurse -Force }
}
