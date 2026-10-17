---
name: cmdpal-store-browser
description: Create or finish a separate Microsoft Store MSIX app draft for a PowerToys Command Palette extension by manually driving the owner's browser. Use for Partner Center name reservation, identity matching, package upload, listing text and screenshots, ratings preparation, and troubleshooting an Incomplete listing. Stops before certification or publication unless separately authorized.
---

# Command Palette Store submission through the browser

## Contract

Prepare **one independent Store app per extension**. This is a browser workflow,
not an unattended publishing bot. Default endpoint: a saved, reviewable draft.
Never equate a local build, package upload validation, a Complete section, or a
successful save with certification or publication.

Read [the runbook](references/runbook.md) before making Partner Center changes.
Read [browser operation](references/browser.md) before computer use.
Read [recovery and lessons](references/recovery.md) **before diagnosing an
Incomplete section or deleting anything**.

Owner-only actions: sign-in, MFA, enrollment/payment/tax/identity information,
accepting agreements, affirming legal age, and granting rights/permissions.
Do not answer these on the owner's behalf. If blocked, preserve the draft and
explain the exact remaining action. Preparation permission is not permission
to click **Submit for certification**, **Publish now**, or delete a submission.
Manual publication hold does not make certification submission harmless.

## Portability and local state

This folder is self-contained. Copy the whole `cmdpal-store-browser` directory
to another project's `.github\skills`, or to the owner's personal
`~\.copilot\skills` directory when they request installation. Its scripts need
Windows PowerShell 7 and built-in Windows UI Automation/.NET assemblies, not
Playwright, browser extensions, Azure credentials, or a particular repository.
Agents with an existing authorized browser-control tool can use that instead.
Do not install additional integrations without approval.

Never reuse another project's publisher GUID, Store ID, package name, COM
class, browser handle, PID, submission ID, release hash, or policy URL.
Discover them each time. Existing repository scripts are project-specific;
this skill does not assume `src\store-apps.json` or a particular build layout.

Create an **untracked, private working directory**, outside shipped package
inputs. Copy [submission.template.json](templates/submission.template.json)
there and populate it from source, owner decisions, and the live portal.
Replace all `<...>` markers. Keep screenshots and receipts there, not secrets.
Do not overwrite the template or check private portal snapshots into Git.
Keep a separate timestamped backup before destructive recovery.

Persist after each section:

- Product identity and observed overview/submission URLs.
- Bundle source commit, CI run, version, exact local path, SHA-256.
- Actual listing values including intentionally blank optional fields, ordered
  keywords, original image files, hashes, dimensions, ordered captions.
- Ratings answers and rationale, owner-consent state, price/markets/device
  families, reviewer notes, publication hold.
- Section status, last readback time, incomplete work, screenshot provenance,
  exact-package tests versus development-build observations.

The JSON is a **local checklist/receipt**, not a Partner Center API payload.
It contains no invented approvals. Unknown and not-run are valid states.

## Required workflow

Follow these gates in order; resume from the first unverified gate.

| Gate | Action | Evidence required before advancing |
| --- | --- | --- |
| 1. Scope | Identify extension, intended publisher/account, requested stop point, repository and source revision. | Owner authorized this app; no suite listing or unrelated edits. |
| 2. Readiness | Inspect manifest, app behavior, assets, direct-launch guidance, legal/privacy materials and CI. | Matching x64/ARM64 packages, correct dependencies/content, truthful listing inputs. |
| 3. Identity | In Apps and games create an **MSIX or PWA app**, reserve its name, read Product identity. | Assigned name/publisher/display name/Store ID recorded and matched exactly to bundle. Rebuild on mismatch. |
| 4. Draft hold | Create/open the draft, record submission URL, set manual publication hold. | Reopen Submission options; the manual radio remains selected. |
| 5. Properties/price | Enter category and live policy/contact URLs; configure owner-approved pricing/markets/device scope. | Reopen and read actual values, not just Complete labels. |
| 6. Package | Upload only this app's approved CI bundle; wait for analysis, expand details, save. | Both **x64 and ARM64**, same version/identity, Desktop targeting; overview Validated/Complete. |
| 7. Media/listing | Capture real CmdPal UI, enter text/media and captions, save. | Local length/asset checks and comparison against a reopened listing. |
| 8. Ratings | Answer from actual content/data flows. Let owner handle agreement/age attestation. | Saved ratings and final preview recorded, or explicit owner-consent blocker. |
| 9. Reviewer/tests | Give reproducible host setup and capability reasons; run permitted exact-package tests. | Actual x64/ARM64 and WACK results, or clearly identified gaps. |
| 10. Review | Reopen every section, compare receipt, verify hold and draft state. | No unresolved listing errors concealed; no certification/publication action without new explicit approval. |

Gate 9 testing can happen earlier. Work on independent draft sections while an
upload processes or owner consent is pending; never navigate away from an
unsaved questionnaire. Use another tab and record which is which.

## Validate text before typing and after restoring a backup

**Copyright and trademark info has a 200-character maximum.** The pilot entered
239 characters; Partner Center saved the text but silently left the listing
Incomplete. Recreating the language restored the invalid text and did not help.
The owner confirmed that shortening it fixed the status. This is a validation
failure first, not evidence that the entire submission is corrupted.

```powershell
# Set these to the actual skill directory and private completed receipt.
$skill = '<absolute skill directory>'
$receipt = '<absolute working directory>\submission.json'
& "$skill\scripts\Test-StoreListing.ps1" -Path $receipt -CheckAssets
```

The script checks listing limits and local PNG dimensions/hashes. It does not
prove rights, identity, certification, network behavior, or portal persistence.
No browser upload or save is performed by this validator.

## Browser discipline

Use the owner's authorized, already-signed-in browser profile. A separate
browser canvas may be signed out and is not automatically the same session.
Never extract cookies/tokens or copy credentials to another browser/tool.

For every mutation: **read -> uniquely identify -> one action -> wait ->
read back**. Obtain current HWNDs and UI selectors; do not reuse examples as
constants. `Save` and navigation are asynchronous. A disabled button is a
reason to investigate, not permission to bypass UI rules.

Use the portable helper only after reading its [recipes](references/browser.md):

```powershell
& "$skill\scripts\Invoke-StoreUi.ps1" -Action Windows -ProcessId <observed-pid>
# Replace the placeholder with the observed window handle.
$window = <observed-hwnd>
& "$skill\scripts\Invoke-StoreUi.ps1" -Window $window -Action Read `
    -Name 'Store listing|Copyright|Save' | ConvertTo-Json -Depth 5
```

If the owner starts using the same window, pause rather than competing for
focus. Never collect unrelated tabs, vaults, browser profiles, or personal files.

## Completion report

Link the observed Partner Center overview URL. State:

- What was actually saved, including package version and both architectures.
- Any owner-only, portal-validation, runtime, or hardware blockers.
- Whether certification was submitted and whether publication occurred.
- Where the backup/receipt lives.

For draft-only work, explicitly say **nothing submitted or published**.
Do not claim "ready for certification" just because the portal enables its
button. Leave the user at the relevant review screen without clicking it.
