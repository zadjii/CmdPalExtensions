#requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Path,
    [switch]$CheckAssets
)

$ErrorActionPreference = 'Stop'
$resolvedPath = (Resolve-Path -LiteralPath $Path).Path
$receipt = Get-Content -Raw -LiteralPath $resolvedPath | ConvertFrom-Json -AsHashtable
if ($receipt.schemaVersion -ne 1 -or $receipt.listing -isnot [System.Collections.IDictionary]) {
    throw 'Expected schemaVersion 1 and a listing object; use submission.template.json.'
}
$listing = $receipt.listing
$errors = [System.Collections.Generic.List[string]]::new()

function Test-Text {
    param($Value, [string]$Label, [int]$Maximum, [bool]$Required = $false)
    if ($Value -isnot [string]) {
        $errors.Add("$Label must be a string (use an empty string for an optional field).")
        return
    }
    if ($Required -and [string]::IsNullOrWhiteSpace($Value)) {
        $errors.Add("$Label is required.")
    }
    if ($Value -match '<[^>]+>') {
        $errors.Add("$Label contains a placeholder or HTML; use completed plain text.")
    }
    if ($Value.Length -gt $Maximum) {
        $errors.Add("$Label is $($Value.Length) characters; maximum $Maximum.")
    }
}

function Test-TextList {
    param($Values, [string]$Label, [int]$Count, [int]$Length)
    if ($Values -isnot [array]) {
        $errors.Add("$Label must be an array.")
        return
    }
    if ($Values.Count -gt $Count) { $errors.Add("$Label has $($Values.Count) entries; maximum $Count.") }
    for ($i = 0; $i -lt $Values.Count; $i++) {
        Test-Text $Values[$i] "$Label[$i]" $Length $true
    }
}

Test-Text $listing.language 'language' 85 $true
Test-Text $listing.productName 'productName' ([int]::MaxValue) $true
Test-Text $listing.description 'description' 10000 $true
Test-Text $listing.releaseNotes 'releaseNotes' 1500
Test-TextList $listing.features 'features' 20 200
Test-Text $listing.shortTitle 'shortTitle' 50
Test-Text $listing.voiceTitle 'voiceTitle' 255
Test-Text $listing.shortDescription 'shortDescription' 1000
Test-TextList $listing.keywords 'keywords' 7 40
if ($listing.keywords -is [array]) {
    $words = [regex]::Matches(($listing.keywords -join ' '), '\S+').Count
    if ($words -gt 21) { $errors.Add("keywords contain $words words; maximum 21.") }
}
Test-Text $listing.copyright 'copyright' 200
Test-Text $listing.additionalLicenseTerms 'additionalLicenseTerms' 10000
Test-Text $listing.developedBy 'developedBy' 255
if ($listing.screenshots -isnot [array] -or $listing.screenshots.Count -eq 0) {
    $errors.Add('At least one screenshot is required; screenshots must be an array.')
} else {
    for ($i = 0; $i -lt $listing.screenshots.Count; $i++) {
        $shot = $listing.screenshots[$i]
        if ($shot -isnot [System.Collections.IDictionary]) {
            $errors.Add("screenshots[$i] must be an object.")
            continue
        }
        Test-Text $shot.path "screenshots[$i].path" ([int]::MaxValue) $true
        Test-Text $shot.caption "screenshots[$i].caption" 200
        Test-Text $shot.provenance "screenshots[$i].provenance" ([int]::MaxValue) $true
    }
}

if ($CheckAssets) {
    if (-not $IsWindows) { throw 'PNG validation uses System.Drawing on Windows; text-only checks work elsewhere.' }
    Add-Type -AssemblyName System.Drawing
    $baseDirectory = Split-Path -Parent $resolvedPath
    function Test-Png {
        param($Asset, [string]$Label, [bool]$Logo = $false)
        if ($Asset -isnot [System.Collections.IDictionary] -or
            $Asset.path -isnot [string] -or [string]::IsNullOrWhiteSpace($Asset.path) -or
            $Asset.path -match '<[^>]+>') {
            $errors.Add("$Label needs a real path.")
            return
        }
        $file = if ([System.IO.Path]::IsPathRooted($Asset.path)) {
            $Asset.path
        } else { Join-Path $baseDirectory $Asset.path }
        if (-not (Test-Path -LiteralPath $file -PathType Leaf)) {
            $errors.Add("$Label file does not exist: $file")
            return
        }
        if ([System.IO.Path]::GetExtension($file) -ine '.png') {
            $errors.Add("$Label must be a PNG.")
            return
        }
        $limit = if ($Logo) { 5MB } else { 50MB }
        if ((Get-Item -LiteralPath $file).Length -ge $limit) {
            $errors.Add("$Label must be smaller than $limit bytes.")
        }
        if ($Asset.sha256 -isnot [string] -or $Asset.sha256 -notmatch '^[0-9a-fA-F]{64}$') {
            $errors.Add("$Label needs its original file SHA-256.")
        } elseif ((Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ine $Asset.sha256) {
            $errors.Add("$Label SHA-256 does not match the recorded file.")
        }
        try { $image = [System.Drawing.Image]::FromFile($file) } catch {
            $errors.Add("$Label cannot be decoded: $($_.Exception.Message)")
            return
        }
        try {
            if ($image.RawFormat.Guid -ne [System.Drawing.Imaging.ImageFormat]::Png.Guid) {
                $errors.Add("$Label is not PNG data.")
            }
            if ($Logo) {
                if ($image.Width -ne 300 -or $image.Height -ne 300) {
                    $errors.Add("$Label must be 300x300; found $($image.Width)x$($image.Height).")
                }
            } elseif ([Math]::Max($image.Width, $image.Height) -lt 1366 -or
                [Math]::Min($image.Width, $image.Height) -lt 768) {
                $errors.Add("$Label must be at least 1366x768 (or portrait equivalent); found $($image.Width)x$($image.Height).")
            }
        } finally { $image.Dispose() }
    }
    if ($listing.screenshots -is [array]) {
        for ($i = 0; $i -lt $listing.screenshots.Count; $i++) {
            Test-Png $listing.screenshots[$i] "screenshots[$i]"
        }
    }
    if ($null -ne $listing.storeLogo) { Test-Png $listing.storeLogo 'storeLogo' $true }
}

if ($errors.Count) { throw ("Listing preflight failed:`n - " + ($errors -join "`n - ")) }
[pscustomobject]@{
    TextLimitsPassed = $true
    AssetsChecked = [bool]$CheckAssets
    CopyrightCharacters = $listing.copyright.Length
    ScreenshotCount = $listing.screenshots.Count
    Scope = 'Local listing checks only; not identity, rights, portal persistence, runtime or certification.'
}
