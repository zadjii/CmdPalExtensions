# Browser and Windows UI Automation recipes

## Choose the control surface

Prefer an available, authorized computer-use/browser tool connected to the
owner's session. A canvas displaying a login page is not the owner's signed-in
Edge window. Discover tool capabilities; never fabricate a `page_id`, debugging
port or session handle. Do not install a tool just because a registry recommends
it, or attempt to transfer authentication cookies.

The pilot used the owner's Edge Beta window through Windows UI Automation.
The supplied `scripts\Invoke-StoreUi.ps1` is a portable replacement for that
session-specific helper. It does not launch a browser, sign in, accept legal
terms, save a listing, or submit anything automatically.

Use an interactive Windows desktop. Keep the owner informed; focus and input
affect their actual machine. Suspend automation if they use the same window.
Use a dedicated working tab and preserve tabs with unsaved rating questionnaires.
Don't change their browser profile, shortcuts, clipboard or global settings.

## Discover and read

Set `$skill` to the directory containing this skill's `SKILL.md`, and `$work` to
an absolute private working directory. These variables do not survive tools
that create a new PowerShell process; set them again in each call.

```powershell
$ui = Join-Path $skill 'scripts\Invoke-StoreUi.ps1'
# Narrow this to the browser the owner identified. Do not dump every open tab.
Get-Process -Name msedge | Where-Object MainWindowHandle -ne 0 |
    Select-Object Id, MainWindowHandle, MainWindowTitle
& $ui -Action Windows -ProcessId $browserProcessId
# Copy the observed handle, not a sample value.
& $ui -Window $window -Action Read -Name 'Hacker|Store listing|Save' |
    ConvertTo-Json -Depth 5
```

Replace `Hacker` with the current product. `Read` returns PowerShell objects;
use `ConvertTo-Json` for readable output, or `Export-Clixml`/JSON for a private
snapshot. It reports value, enabled/offscreen state, checkbox state, selected
radio/list state, rectangle and HWND where available. It skips password fields,
but ordinary inputs may still contain private data: read only the needed page.

Selectors:

- `-Name` is a **regex for Read**, an **exact string for actions**.
- `-Id` and `-Type` narrow controls. IDs may change after reload.
- `-WithinGroup 'exact question text'` scopes repeated Yes/No choices.
- `-Native` reads the window instead of a web Document. Use for native dialogs
  or content outside the first Document (seen with reviewer notes).
- `-IncludeUnnamed` exposes unnamed upload/delete buttons. Identify them by
  their surrounding tree/rectangle before use, not by "first button".
- `-Index` comes from the last snapshot of the same scope. Actions still require
  an exact Name or Id. Never use it after a DOM update without reading again.
- `-ExpectedWindowTitle` adds a regex assertion before acting.

The helper refuses ambiguous Documents/controls, disabled/password fields and
stale indices. It does not prove that an exact title or enabled button represents
the intended action: inspect the page. Distinguish **Save** from **Submit for
certification**, and **Remove language** from **Delete submission**.

## Edit one field, verify it

Observed IDs are useful search hints, not guaranteed API contracts:

| Surface | Pilot hint |
| --- | --- |
| Description | `description-required` |
| Features | `feature-0`, `feature-1`, then Add more |
| Short description | `shortDescription` |
| Copyright | `trademark` |
| Developer | `devStudio` |
| Release notes / additional terms | `releaseNotes`, `additionalLicenseTerms` |
| Caption popup | `captionTextArea`, OK |
| Manual hold | `radioReleaseDate_manual` |
| Reviewer notes | `certificationNotesDescription`, Save description |
| CmdPal query | `MainSearchBox` |

```powershell
& $ui -Window $window -Action Read -Id trademark
& $ui -Window $window -Action Set -Id trademark -Type Edit -Text $copyright
& $ui -Window $window -Action Read -Id trademark
```

`Set` uses ValuePattern and passes literal text, including backslashes. Prefer
it to keyboard typing. Explicit `-Text ''` clears an optional field. For a
ComboBox, Expand, read the actual ListItems, Select the exact item, then Escape
if the dropdown stays open. Allow follow-on fields time to enable.

`SetChecked -Checked $true/$false` is idempotent, unlike blindly toggling. Never
use it for owner-only IARC agreements/age attestation or unsupported rights
declarations. Read the persisted state after navigation as well as immediately.

`Keys` uses SendKeys notation (`^l` Ctrl+L, `^2` second tab, `{ENTER}`, `{ESC}`).
Use it only for shortcuts on the already verified foreground window. Do **not**
concatenate arbitrary text or file paths to shortcut strings. `%`, `+`, `^`,
`~`, parentheses and braces are SendKeys metacharacters.

`TypeText` types literal characters into a uniquely selected focused control,
escaping those metacharacters; it is a fallback when ValuePattern is absent.
It checks that keyboard focus stays on that exact control. If a container
delegates focus to a child, select the actual child input instead.
It **appends** at the caret. To replace content, focus the exact input and select
all first. Do not use either typing action for credentials.

```powershell
# Native chooser example; obtain all selectors from the live dialog first.
& $ui -Window $dialogWindow -Native -Action Read -Type Edit
& $ui -Window $dialogWindow -Native -Action Set `
    -Id $fileNameId -Type Edit -Text $absoluteFilePath
# If Set is unavailable, do not force it: focus the observed input, Ctrl+A,
# then TypeText into that same input, and read back the complete path.
```

## File upload: handle the modal dialog separately

1. Read the upload area's label (Desktop screenshots vs 300x300 tile vs bundle).
2. Inspect unnamed controls with their parent/siblings. A small trash button and
   large plus tile can both have empty accessible names. **The first button can
   delete an image.** Do not infer purpose from an ordinal alone.
3. Click the observed upload control once. Wait for a **native Open dialog**.
4. Obtain its actual HWND from the browser's descendant Windows:

   ```powershell
   Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
   $root = [System.Windows.Automation.AutomationElement]::FromHandle([IntPtr]$window)
   $condition = [System.Windows.Automation.PropertyCondition]::new(
       [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
       [System.Windows.Automation.ControlType]::Window)
   $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $condition) |
       ForEach-Object {
           [pscustomobject]@{ Name = $_.Current.Name; Window = $_.Current.NativeWindowHandle }
       }
   ```

5. Send input to that HWND, **not the browser behind the modal**. Read file-name
   controls, enter the complete absolute path with ValuePattern if supported,
   read it back, and click the dialog's Open. If only a directory was navigated,
   Select the exact visible filename ListItem, then click Open. Never select
   a neighboring bundle/image. Native file names may expose `System.ItemNameDisplay`
   edits inside the file list; those are **not** the file-name input.
6. If another dialog appears (path missing, overwrite, etc.), read it before
   dismissing. The pilot's fast shortcut+path typing lost characters. A missing
   path is not proof the source file is absent; inspect the actual typed value.
7. Upload one file at a time. Multi-file selection in the pilot retained only
   one image. Wait for the screenshot count/preview or package analysis result.
8. Re-discover HWNDs after each chooser closes. Never cache dialog handles.

Use bounded waits with explicit timeouts and read errors. If an upload is still
processing at timeout, inspect/wait for that upload rather than uploading a
duplicate. A blank image preview or temporary Desktop (0) is not yet success.

For example, after uploading the first image to an English UI:

```powershell
$deadline = (Get-Date).AddSeconds(90)
do {
    Start-Sleep -Seconds 2
    $panels = @(& $ui -Window $window -Action Read -Type Pane -Name '^Desktop')
    $complete = $panels.Count -eq 1 -and $panels[0].Name -match '\(1\)'
} while (-not $complete -and (Get-Date) -lt $deadline)
if (-not $complete) { throw 'Image is not confirmed uploaded. Inspect errors; do not upload again blindly.' }
```

Use the observed language label and expected count for subsequent images.

After adding a screenshot, click its own caption control, fill the popup and
click **OK**, then save the whole listing. Reopen to verify caption persistence.

## Save and readback

Scroll Save into view when necessary, click once, and wait for navigation or
the saving indicator. Then navigate away and reopen the section. An immediate
post-click snapshot may still be the old page. Verify URL/title and controls
before taking the next action. Some navigation took multiple seconds.

Use observed links from Read rather than constructing URLs. The pilot used
overview, identity, availability, properties, ageratings, packages,
managelanguages, listings, options and supplemental reviewer-information pages;
different experiences/locales may route differently.

If foreground focus fails, first dismiss CmdPal normally if it owns the
foreground. The helper tries AppActivate plus SetForegroundWindow and verifies
the result; repeated retries are not a solution to user activity. Stop and
inspect. A successful API return or a window titled "Command Palette" does not
prove it has visible content.

## Actual CmdPal screenshots

1. Read only the chosen extension's installed package/version/architecture and
   running executable path. Identify legacy/development registrations and avoid
   claiming they are the release bundle. Do not remove anything without consent.
2. Show the palette using the supported URI:

   ```powershell
   Start-Process 'x-cmdpal:'
   ```

   If needed use `Start-Process explorer.exe -ArgumentList 'x-cmdpal:'`. Injected
   Win+Alt+Space may be ignored by the low-level hotkey hook. AppsFolder activation
   can start the host without showing its main UI. Don't change user shortcuts.
3. Discover the actual process and its top-level windows. **PowerDock is not
   the palette**. Re-read after activation; hidden/stale windows can have no
   controls. Set `MainSearchBox` to this extension's name and select the real
   provider command, not the installed-app search result.
4. If duplicate provider results appear, establish the running executable/
   package source. Don't assume first result means the new version.
5. To enlarge the **real** window for a sufficiently large capture, when
   supported:

   ```powershell
   $palette = [System.Windows.Automation.AutomationElement]::FromHandle([IntPtr]$paletteWindow)
   $original = $palette.Current.BoundingRectangle
   $transform = $palette.GetCurrentPattern([System.Windows.Automation.TransformPattern]::Pattern)
   if (-not $transform.Current.CanResize) { throw 'Use another supported capture layout.' }
   $transform.Resize(1440, 1000)
   # Inspect actual layout/bounds, capture, then restore:
   # $transform.Resize($original.Width, $original.Height)
   ```

6. Capture only safe content using `Screenshot -ScreenshotPath $newAbsolutePath`.
   It captures **screen pixels**, not a hidden render surface: foreground,
   overlays, transparent margins and off-monitor bounds matter. Inspect the
   image; a blank/stale capture is not acceptable. Crop unrelated margins using
   a normal image tool or System.Drawing, retaining all meaningful product UI.
   Do not paint over or fabricate content. Restore original window size.
7. Record the source version and pixel dimensions, compute SHA-256, validate
   local assets, then upload. Keep useful originals/receipts; remove temporary
   portal screenshots containing account information when no longer needed.

## Backup recipe

Before an authorized removal, reread **current** values (the owner may have
changed them since the last receipt). Save the complete listing receipt and
focused UI snapshot; include intentionally empty fields. Copy original PNGs to
a new backup directory and record their hashes and ordered captions. Compare
all fields and assets after restoration and reload. Do not overwrite a backup.

```powershell
# Run only on the identified listing page, not an account/settings page.
$snapshot = @(& $ui -Window $window -Action Read)
$snapshotPath = Join-Path $work ('listing-snapshot-' + (Get-Date -Format yyyyMMdd-HHmmss) + '.json')
if (Test-Path -LiteralPath $snapshotPath) { throw 'Choose a new backup name.' }
$snapshot | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $snapshotPath -Encoding utf8
# Populate the portable receipt from these actual values, copy the original
# images beside it, and record Get-FileHash -Algorithm SHA256 for each.
```

The local JSON validator cannot extract a portal image. If the original files
are missing, use the portal's supported export/download flow or ask the owner.
Do not remove the listing until media and text are restorable. Native listing
CSV export/import is another supported option; inspect the export rather than
assuming it includes image binaries.
