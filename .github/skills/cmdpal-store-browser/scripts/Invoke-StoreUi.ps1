#requires -Version 7.0
[CmdletBinding()]
param(
    [ValidateSet('Windows', 'Read', 'Click', 'Set', 'Select', 'Expand', 'SetChecked',
        'Focus', 'Scroll', 'Keys', 'TypeText', 'Screenshot')]
    [string]$Action = 'Read',
    [long]$Window,
    [int]$ProcessId,
    [string]$Name,
    [string]$Id,
    [string]$Type,
    [int]$Index = -1,
    [string]$WithinGroup,
    [string]$ExpectedWindowTitle,
    [string]$Text,
    [bool]$Checked,
    [switch]$Native,
    [switch]$IncludeUnnamed,
    [string]$ScreenshotPath
)

$ErrorActionPreference = 'Stop'
if (-not $IsWindows) { throw 'UI Automation requires an interactive Windows session.' }
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Windows.Forms
if (-not ('StoreUiNative' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class StoreUiNative {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint x, uint y, uint data, UIntPtr extra);
}
'@
}
$descendants = [System.Windows.Automation.TreeScope]::Descendants
$all = [System.Windows.Automation.Condition]::TrueCondition
if ($Action -eq 'Windows') {
    if ($ProcessId -le 0) { throw 'Windows requires an explicitly selected ProcessId.' }
    $condition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $ProcessId)
    foreach ($item in [System.Windows.Automation.AutomationElement]::RootElement.FindAll(
        [System.Windows.Automation.TreeScope]::Children, $condition)) {
        [pscustomobject]@{ Name = $item.Current.Name; Window = $item.Current.NativeWindowHandle
            ProcessId = $item.Current.ProcessId; Offscreen = $item.Current.IsOffscreen }
    }
    return
}
if ($Window -le 0) { throw 'Supply the observed Window handle; no default browser is assumed.' }
$root = [System.Windows.Automation.AutomationElement]::FromHandle([IntPtr]$Window)
if ($null -eq $root) { throw 'Window no longer exists. Discover its handle again.' }
if ($ExpectedWindowTitle -and $root.Current.Name -notmatch $ExpectedWindowTitle) {
    throw "Unexpected window title: $($root.Current.Name)"
}
function Assert-Foreground {
    if ([StoreUiNative]::GetForegroundWindow() -ne [IntPtr]$Window) {
        throw 'Foreground changed. Stop input and inspect the window; do not retry blindly.'
    }
}
if ([StoreUiNative]::GetForegroundWindow() -ne [IntPtr]$Window) {
    [void][StoreUiNative]::SetForegroundWindow([IntPtr]$Window)
    if ([StoreUiNative]::GetForegroundWindow() -ne [IntPtr]$Window) {
        $shell = New-Object -ComObject WScript.Shell
        [void]$shell.AppActivate($root.Current.ProcessId)
        Start-Sleep -Milliseconds 200
        [void][StoreUiNative]::SetForegroundWindow([IntPtr]$Window)
    }
    Assert-Foreground
}
if ($Action -eq 'Keys') {
    if (-not $PSBoundParameters.ContainsKey('Text')) { throw 'Keys requires Text in SendKeys syntax.' }
    Assert-Foreground
    [System.Windows.Forms.SendKeys]::SendWait($Text)
    return
}
if ($Action -eq 'Screenshot') {
    if (-not $ScreenshotPath -or -not [System.IO.Path]::IsPathRooted($ScreenshotPath)) {
        throw 'Screenshot requires an explicit absolute ScreenshotPath outside the repository.'
    }
    if (Test-Path -LiteralPath $ScreenshotPath) { throw 'ScreenshotPath already exists; choose a new file.' }
    Add-Type -AssemblyName System.Drawing
    Start-Sleep -Milliseconds 300
    Assert-Foreground
    $rect = $root.Current.BoundingRectangle
    if ($rect.IsEmpty -or $rect.Width -le 0 -or $rect.Height -le 0 -or $root.Current.IsOffscreen) {
        throw 'Window is not visibly capturable.'
    }
    $bitmap = [System.Drawing.Bitmap]::new([int]$rect.Width, [int]$rect.Height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.CopyFromScreen([int]$rect.X, [int]$rect.Y, 0, 0, $bitmap.Size)
        $bitmap.Save($ScreenshotPath, [System.Drawing.Imaging.ImageFormat]::Png)
    } finally { $graphics.Dispose(); $bitmap.Dispose() }
    Get-Item -LiteralPath $ScreenshotPath | Select-Object FullName, Length
    return
}
$document = $root
if (-not $Native) {
    $condition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::Document)
    $documents = $root.FindAll($descendants, $condition)
    if ($documents.Count -ne 1) {
        throw "Expected one web Document, found $($documents.Count). Inspect with -Native and scope explicitly."
    }
    $document = $documents[0]
}
if ($WithinGroup) {
    $groups = @($document.FindAll($descendants, $all) | Where-Object {
        $_.Current.ControlType -eq [System.Windows.Automation.ControlType]::Group -and
        $_.Current.Name -eq $WithinGroup
    })
    if ($groups.Count -ne 1) { throw "Expected one named group, found $($groups.Count)." }
    $document = $groups[0]
}
$nodes = $document.FindAll($descendants, $all)
if ($Action -eq 'Read') {
    for ($i = 0; $i -lt $nodes.Count; $i++) {
        $node = $nodes[$i]
        $current = $node.Current
        $controlType = $current.ControlType.ProgrammaticName.Replace('ControlType.', '')
        if ($current.IsPassword) { continue }
        if (-not $IncludeUnnamed -and -not $current.Name -and $controlType -notin 'Edit', 'ComboBox') { continue }
        if ($PSBoundParameters.ContainsKey('Name') -and $current.Name -notmatch $Name) { continue }
        if ($PSBoundParameters.ContainsKey('Id') -and $current.AutomationId -ne $Id) { continue }
        if ($Type -and $controlType -ne $Type) { continue }
        $value = $null; $state = $null; $selected = $null; $pattern = $null
        if ($node.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern, [ref]$pattern)) {
            $value = $pattern.Current.Value
        }
        if ($node.TryGetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern, [ref]$pattern)) {
            $state = $pattern.Current.ToggleState.ToString()
        }
        if ($node.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$pattern)) {
            $selected = $pattern.Current.IsSelected
        }
        [pscustomobject]@{
            Index = $i; Type = $controlType; Name = $current.Name; Id = $current.AutomationId
            Value = $value; Checked = $state; Selected = $selected
            Enabled = $current.IsEnabled; Offscreen = $current.IsOffscreen
            Window = $current.NativeWindowHandle; Bounds = $current.BoundingRectangle.ToString()
        }
    }
    return
}
$hasName = $PSBoundParameters.ContainsKey('Name')
$hasId = $PSBoundParameters.ContainsKey('Id')
if (-not $hasName -and -not $hasId) { throw 'Actions require an exact Name or Id, even with Index.' }
$matching = @($nodes | Where-Object {
    (-not $hasName -or $_.Current.Name -eq $Name) -and
    (-not $hasId -or $_.Current.AutomationId -eq $Id) -and
    (-not $Type -or $_.Current.ControlType.ProgrammaticName -eq "ControlType.$Type")
})
if ($Index -ge 0) {
    if ($Index -ge $nodes.Count -or $nodes[$Index] -notin $matching) {
        throw 'Stale index or selector. Read the UI again.'
    }
    $node = $nodes[$Index]
} elseif ($matching.Count -eq 1) { $node = $matching[0] } else {
    throw "Expected one matching control, found $($matching.Count). Narrow the selector."
}
if (-not $node.Current.IsEnabled -or $node.Current.IsPassword) { throw 'Control is disabled or password-protected.' }
Assert-Foreground
switch ($Action) {
    'Set' {
        if (-not $PSBoundParameters.ContainsKey('Text')) { throw 'Set requires Text; an explicit empty string clears the field.' }
        $pattern = $node.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
        if ($pattern.Current.IsReadOnly) { throw 'Field is read-only.' }
        $pattern.SetValue($Text)
    }
    'TypeText' {
        if (-not $PSBoundParameters.ContainsKey('Text')) { throw 'TypeText requires literal Text.' }
        $node.SetFocus()
        foreach ($character in $Text.ToCharArray()) {
            Assert-Foreground
            $focused = [System.Windows.Automation.AutomationElement]::FocusedElement
            if ($null -eq $focused -or -not $focused.Equals($node)) {
                throw 'Input focus is not on the selected control. Read the dialog and select its actual input.'
            }
            $key = switch ([string]$character) {
                '{' { '{{}' }; '}' { '{}}' }; '+' { '{+}' }; '^' { '{^}' }
                '%' { '{%}' }; '~' { '{~}' }; '(' { '{(}' }; ')' { '{)}' }
                '[' { '{[}' }; ']' { '{]}' }; "`n" { '{ENTER}' }; "`r" { '' }
                default { [string]$character }
            }
            if ($key) { [System.Windows.Forms.SendKeys]::SendWait($key) }
        }
    }
    'Select' { $node.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select() }
    'Expand' { $node.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Expand() }
    'Focus' { $node.SetFocus() }
    'Scroll' { $node.GetCurrentPattern([System.Windows.Automation.ScrollItemPattern]::Pattern).ScrollIntoView() }
    'SetChecked' {
        if (-not $PSBoundParameters.ContainsKey('Checked')) { throw 'SetChecked requires -Checked $true or $false.' }
        $pattern = $node.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
        $desired = if ($Checked) { 'On' } else { 'Off' }
        for ($attempt = 0; $attempt -lt 3 -and $pattern.Current.ToggleState.ToString() -ne $desired; $attempt++) {
            $pattern.Toggle()
            Start-Sleep -Milliseconds 300
        }
        if ($pattern.Current.ToggleState.ToString() -ne $desired) { throw 'Checkbox did not reach the requested state.' }
    }
    'Click' {
        $pattern = $null
        if ($node.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$pattern)) {
            $pattern.Invoke()
        } else {
            if ($node.Current.IsOffscreen) { throw 'Scroll the control into view before clicking.' }
            $point = [System.Windows.Point]::new()
            if (-not $node.TryGetClickablePoint([ref]$point)) { throw 'No safe clickable point; inspect the UI.' }
            [void][StoreUiNative]::SetCursorPos([int]$point.X, [int]$point.Y)
            Assert-Foreground
            [StoreUiNative]::mouse_event(2, 0, 0, 0, [UIntPtr]::Zero)
            [StoreUiNative]::mouse_event(4, 0, 0, 0, [UIntPtr]::Zero)
        }
    }
}
Write-Verbose "$Action executed. Read the resulting UI to verify persistence."
