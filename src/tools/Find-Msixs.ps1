
$gitRoot = git rev-parse --show-toplevel
$gitRoot
$winui3Apps = Join-Path $gitRoot "x64\Release\WinUI3Apps"
$winui3Apps
$msixs = Get-ChildItem -Path $winui3Apps -Recurse -File -Filter "*.msix" -exclude "Microsoft.WindowsAppRuntime.1.6.msix"
# $msixs

$DestinationFolder = Join-Path $gitRoot "x64\tmp"

if(Test-Path $DestinationFolder) {
    Remove-Item -Path $DestinationFolder -Recurse -Force | Out-Null
}
if(-not (Test-Path $DestinationFolder)) {
    New-Item -ItemType Directory -Path $DestinationFolder -Force | Out-Null
}

write-host "Copying msix's to $DestinationFolder..."

foreach($msix in $msixs) {
    Copy-Item -Path $msix -Destination $DestinationFolder -Force
}

write-host "Done"
