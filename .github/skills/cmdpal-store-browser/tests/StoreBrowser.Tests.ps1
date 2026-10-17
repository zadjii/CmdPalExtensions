#requires -Version 7.0
[CmdletBinding()]
param(
    [switch]$ExerciseUi,
    [switch]$UiFixture,
    [string]$FixtureStatePath
)
$ErrorActionPreference = 'Stop'

if ($UiFixture) {
    Add-Type -AssemblyName System.Windows.Forms, System.Drawing
    $form = [System.Windows.Forms.Form]::new()
    $form.Text = 'Store skill disposable UI test'
    $form.Size = [System.Drawing.Size]::new(500, 300)
    $form.StartPosition = 'CenterScreen'
    $text = [System.Windows.Forms.TextBox]::new()
    $text.Name = 'ListingText'
    $text.Text = 'Initial value'
    $text.SetBounds(20, 20, 430, 25)
    $checkbox = [System.Windows.Forms.CheckBox]::new()
    $checkbox.Name = 'DesktopCheckbox'
    $checkbox.Text = 'Desktop'
    $checkbox.SetBounds(20, 60, 200, 25)
    $password = [System.Windows.Forms.TextBox]::new()
    $password.Name = 'PasswordBox'
    $password.UseSystemPasswordChar = $true
    $password.Text = 'do-not-read-this-fixture-value'
    $password.SetBounds(20, 100, 430, 25)
    $disabled = [System.Windows.Forms.Button]::new()
    $disabled.Name = 'DisabledAction'
    $disabled.Text = 'Disabled'
    $disabled.Enabled = $false
    $disabled.SetBounds(20, 140, 120, 25)
    $first = [System.Windows.Forms.Button]::new()
    $first.Text = 'Duplicate'
    $first.SetBounds(160, 140, 120, 25)
    $second = [System.Windows.Forms.Button]::new()
    $second.Text = 'Duplicate'
    $second.SetBounds(300, 140, 120, 25)
    $form.Controls.AddRange(@($text, $checkbox, $password, $disabled, $first, $second))
    $form.Add_Shown({
        [pscustomobject]@{ Window = $form.Handle.ToInt64(); ProcessId = $PID } |
            ConvertTo-Json | Set-Content -LiteralPath $FixtureStatePath -Encoding utf8
        $form.Activate()
    })
    try { [void]$form.ShowDialog() } finally { $form.Dispose() }
    return
}

$skill = Split-Path -Parent $PSScriptRoot
$validator = Join-Path $skill 'scripts\Test-StoreListing.ps1'
$ui = Join-Path $skill 'scripts\Invoke-StoreUi.ps1'
$template = Join-Path $skill 'templates\submission.template.json'
$temp = Join-Path ([System.IO.Path]::GetTempPath()) ('store-skill-test-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temp | Out-Null
$script:passed = 0
$fixture = $null
function Assert-True {
    param([bool]$Value, [string]$Message)
    if (-not $Value) { throw $Message }
    $script:passed++
}
function Assert-Fails {
    param([scriptblock]$Action, [string]$Pattern)
    $caught = $null
    try { & $Action | Out-Null } catch { $caught = $_ }
    if ($null -eq $caught -or $caught.Exception.Message -notmatch $Pattern) {
        throw "Expected failure matching '$Pattern', got '$caught'."
    }
    $script:passed++
}
function New-Receipt {
    $value = Get-Content -Raw -LiteralPath $template | ConvertFrom-Json -AsHashtable
    $value.listing.language = 'en-us'
    $value.listing.productName = 'Example for Command Palette'
    $value.listing.description = 'A real extension description. Requires PowerToys Command Palette.'
    $value.listing.features = @('A useful feature')
    $value.listing.keywords = @('utility', 'Command Palette')
    $value.listing.copyright = 'Independent extension. MIT-licensed.'
    $value.listing.developedBy = 'Example publisher'
    $value.listing.screenshots = @(@{
        path = 'screen.png'; caption = 'Real extension UI'; provenance = 'Disposable test fixture, not Store art.'
        sha256 = $script:screenHash
    })
    $value.listing.storeLogo = @{ path = 'logo.png'; sha256 = $script:logoHash }
    return $value
}
function Save-Receipt {
    param($Value)
    $Value | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $receiptPath -Encoding utf8
}

try {
    foreach ($scriptPath in @($validator, $ui, $PSCommandPath)) {
        $parseErrors = $null; $tokens = $null
        [void][System.Management.Automation.Language.Parser]::ParseFile($scriptPath, [ref]$tokens, [ref]$parseErrors)
        Assert-True ($parseErrors.Count -eq 0) "PowerShell parse failure: $scriptPath"
    }
    if (-not $IsWindows) { throw 'These tests include PNG checks and require Windows.' }
    Add-Type -AssemblyName System.Drawing
    foreach ($spec in @(@('screen.png', 1366, 768), @('portrait.png', 768, 1366),
        @('small.png', 1150, 945), @('logo.png', 300, 300))) {
        $bitmap = [System.Drawing.Bitmap]::new($spec[1], $spec[2])
        try { $bitmap.Save((Join-Path $temp $spec[0]), [System.Drawing.Imaging.ImageFormat]::Png) }
        finally { $bitmap.Dispose() }
    }
    $script:screenHash = (Get-FileHash -LiteralPath (Join-Path $temp 'screen.png')).Hash
    $script:logoHash = (Get-FileHash -LiteralPath (Join-Path $temp 'logo.png')).Hash
    $receiptPath = Join-Path $temp 'receipt.json'
    Save-Receipt (New-Receipt)
    $result = & $validator -Path $receiptPath -CheckAssets
    Assert-True ($result.TextLimitsPassed -and $result.AssetsChecked) 'Valid listing did not pass.'
    $value = New-Receipt
    $value.listing.copyright = 'x' * 200
    Save-Receipt $value
    Assert-True ((& $validator -Path $receiptPath).CopyrightCharacters -eq 200) '200 characters should pass.'
    foreach ($length in 201, 239) {
        $value.listing.copyright = 'x' * $length
        Save-Receipt $value
        Assert-Fails { & $validator -Path $receiptPath } 'copyright.*maximum 200'
    }
    foreach ($case in @(
        @{ Change = { param($v) $v.listing.description = ' ' }; Error = 'description is required' },
        @{ Change = { param($v) $v.listing.description = '<placeholder>' }; Error = 'placeholder' },
        @{ Change = { param($v) $v.listing.description = 'x' * 10001 }; Error = 'maximum 10000' },
        @{ Change = { param($v) $v.listing.features = @('x' * 201) }; Error = 'maximum 200' },
        @{ Change = { param($v) $v.listing.features = @('x') * 21 }; Error = 'maximum 20' },
        @{ Change = { param($v) $v.listing.keywords = @('x') * 8 }; Error = 'maximum 7' },
        @{ Change = { param($v) $v.listing.keywords = @('x' * 41) }; Error = 'maximum 40' },
        @{ Change = { param($v) $v.listing.keywords = @('a b c d', 'e f g h', 'i j k l', 'm n o p', 'q r s t u v') }; Error = 'maximum 21' },
        @{ Change = { param($v) $v.listing.shortTitle = 'x' * 51 }; Error = 'maximum 50' },
        @{ Change = { param($v) $v.listing.developedBy = 'x' * 256 }; Error = 'maximum 255' },
        @{ Change = { param($v) $v.listing.screenshots = @() }; Error = 'At least one' },
        @{ Change = { param($v) $v.listing.screenshots[0].caption = 'x' * 201 }; Error = 'caption.*maximum 200' },
        @{ Change = { param($v) $v.listing.screenshots[0].provenance = '' }; Error = 'provenance is required' },
        @{ Change = { param($v) $v.listing.features = 'not an array' }; Error = 'features must be an array' }
    )) {
        $value = New-Receipt
        & $case.Change $value
        Save-Receipt $value
        Assert-Fails { & $validator -Path $receiptPath } $case.Error
    }
    $value = New-Receipt
    $value.listing.keywords = @('a b c', 'd e f', 'g h i', 'j k l', 'm n o', 'p q r', 's t u')
    Save-Receipt $value
    Assert-True ((& $validator -Path $receiptPath).TextLimitsPassed) 'Exactly 21 keyword words should pass.'
    $value = New-Receipt
    $value.listing.screenshots[0].path = 'portrait.png'
    $value.listing.screenshots[0].sha256 = (Get-FileHash -LiteralPath (Join-Path $temp 'portrait.png')).Hash
    Save-Receipt $value
    Assert-True ((& $validator -Path $receiptPath -CheckAssets).AssetsChecked) 'Portrait dimensions should pass.'
    $value.listing.screenshots[0].path = 'small.png'
    $value.listing.screenshots[0].sha256 = (Get-FileHash -LiteralPath (Join-Path $temp 'small.png')).Hash
    Save-Receipt $value
    Assert-Fails { & $validator -Path $receiptPath -CheckAssets } 'at least 1366x768'
    $value = New-Receipt
    $value.listing.storeLogo.path = 'screen.png'
    $value.listing.storeLogo.sha256 = $script:screenHash
    Save-Receipt $value
    Assert-Fails { & $validator -Path $receiptPath -CheckAssets } 'must be 300x300'
    $value = New-Receipt
    $value.listing.screenshots[0].sha256 = '0' * 64
    Save-Receipt $value
    Assert-Fails { & $validator -Path $receiptPath -CheckAssets } 'SHA-256 does not match'
    $value.listing.screenshots[0].path = 'missing.png'
    Save-Receipt $value
    Assert-Fails { & $validator -Path $receiptPath -CheckAssets } 'file does not exist'
    $value.listing.screenshots[0].path = 'corrupt.png'
    'not a PNG' | Set-Content -LiteralPath (Join-Path $temp 'corrupt.png')
    $value.listing.screenshots[0].sha256 = (Get-FileHash -LiteralPath (Join-Path $temp 'corrupt.png')).Hash
    Save-Receipt $value
    Assert-Fails { & $validator -Path $receiptPath -CheckAssets } 'cannot be decoded'
    Assert-Fails { & $validator -Path $template } 'placeholder'

    if ($ExerciseUi) {
        $state = Join-Path $temp 'fixture.json'
        $fixture = Start-Process -FilePath (Join-Path $PSHOME 'pwsh.exe') -PassThru -ArgumentList @(
            '-NoProfile', '-STA', '-File', "`"$PSCommandPath`"", '-UiFixture', '-FixtureStatePath', "`"$state`"")
        for ($i = 0; $i -lt 40 -and -not (Test-Path -LiteralPath $state); $i++) {
            if ($fixture.HasExited) { throw 'UI fixture exited before showing its window.' }
            Start-Sleep -Milliseconds 250
        }
        if (-not (Test-Path -LiteralPath $state)) { throw 'Timed out waiting for disposable UI fixture.' }
        $fixtureInfo = Get-Content -Raw -LiteralPath $state | ConvertFrom-Json
        $window = $fixtureInfo.Window
        $target = @{ Window = $window; Native = $true; ExpectedWindowTitle = '^Store skill disposable UI test$' }
        $windows = @(& $ui -Action Windows -ProcessId $fixture.Id)
        Assert-True ($window -in $windows.Window) 'Fixture window discovery failed.'
        $snapshot = @(& $ui @target -Action Read)
        Assert-True (-not ($snapshot | Where-Object Id -eq 'PasswordBox')) 'Password field leaked into Read.'
        & $ui @target -Action Set -Id ListingText -Type Edit -Text 'Literal + value'
        Assert-True ((& $ui @target -Action Read -Id ListingText).Value -ceq 'Literal + value') 'Set/read failed.'
        & $ui @target -Action Set -Id ListingText -Type Edit -Text ''
        Assert-True ((& $ui @target -Action Read -Id ListingText).Value -ceq '') 'Explicit clearing failed.'
        $literal = 'C:\store+assets\100% (test) [x] ^ {draft}~.png'
        & $ui @target -Action TypeText -Id ListingText -Type Edit -Text $literal
        Assert-True ((& $ui @target -Action Read -Id ListingText).Value -ceq $literal) 'Literal typing corrupted metacharacters.'
        & $ui @target -Action SetChecked -Id DesktopCheckbox -Checked $true
        & $ui @target -Action SetChecked -Id DesktopCheckbox -Checked $true
        Assert-True ((& $ui @target -Action Read -Id DesktopCheckbox).Checked -eq 'On') 'Checkbox operation is not idempotent.'
        & $ui @target -Action SetChecked -Id DesktopCheckbox -Checked $false
        Assert-True ((& $ui @target -Action Read -Id DesktopCheckbox).Checked -eq 'Off') 'Unchecking failed.'
        Assert-Fails { & $ui @target -Action Set -Id PasswordBox -Text 'x' } 'password'
        Assert-Fails { & $ui @target -Action Click -Id DisabledAction } 'disabled'
        Assert-Fails { & $ui @target -Action Click -Name Duplicate -Type Button } 'found 2'
        Assert-Fails { & $ui @target -Action Set -Id ListingText -Index 9999 -Text 'x' } 'Stale index'
        Assert-Fails { & $ui -Window $window -Native -ExpectedWindowTitle '^Wrong title$' -Action Read } 'Unexpected window'
        Assert-Fails { & $ui @target -Action Click } 'exact Name or Id'
        $capture = Join-Path $temp 'fixture-capture.png'
        & $ui @target -Action Screenshot -ScreenshotPath $capture | Out-Null
        Assert-True (Test-Path -LiteralPath $capture) 'Screenshot was not written.'
        Assert-Fails { & $ui @target -Action Screenshot -ScreenshotPath $capture } 'already exists'
    }
    "Passed $script:passed checks. UI exercised: $([bool]$ExerciseUi). No Partner Center actions performed."
} finally {
    if ($null -ne $fixture -and -not $fixture.HasExited) {
        [void]$fixture.CloseMainWindow()
        if (-not $fixture.WaitForExit(3000)) { Stop-Process -Id $fixture.Id -Force }
    }
    Remove-Item -LiteralPath $temp -Recurse -Force
}
