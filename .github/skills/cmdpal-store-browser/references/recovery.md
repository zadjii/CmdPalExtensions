# Recovery order and pilot lessons

## First classify what is incomplete

Read the overview and the specific language row. Distinguish:

- Package analysis still running or validation failed.
- Properties/availability required fields not saved.
- Age ratings staged but blocked on owner agreement.
- Store listings -> one particular language Incomplete.
- Submission options missing restricted-capability information.

Don't diagnose all five as the same Partner Center bug. Reopen the failing
section and compare actual persisted values to the receipt.

## Silent Store listing validation: check this FIRST

1. Count **all** text fields against the limits in the runbook. Run
   `Test-StoreListing.ps1 -CheckAssets` against current values, not old drafts.
2. Especially check **copyright/trademark <= 200 characters**. The UI can save
   overlong content without displaying the error that blocks completion.
3. Check feature/caption lengths and total keyword count/word count. Optional
   does not mean unvalidated when populated.
4. Confirm there is a real screenshot under **Desktop**, not only a Store
   logo; verify dimensions, format, count, upload completion and saved captions.
5. Check no partially uploaded/unused trailer is stuck in processing. Don't
   add Xbox art when only Desktop is supported.
6. Check every selected language has a description/screenshot. Remove only
   genuinely unintended languages with owner permission; do not modify package
   language declarations just to evade a listing requirement.
7. Check reserved product name selection, save the correct language, navigate
   away and reload. Confirm price `0` after reopening, if availability is the
   failing section instead.

**Confirmed pilot root cause:** 239 characters in the copyright field exceeded
the documented 200. Language deletion/recreation restored the same invalid
value, so the symptom persisted. On 2026-10-07 the owner shortened it and
confirmed that the listing completed. Do not describe this pilot as an
unresolved backend bug.

Useful sources:

- [Official 200-character limit](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/add-additional-information#copyright-and-trademark-info)
- [Q&A: same hidden-error symptom, quoting support](https://learn.microsoft.com/en-us/answers/questions/2102499/partner-center-store-listing-incomplete)
- [Q&A: other reports with no visible error](https://learn.microsoft.com/en-us/answers/questions/2285377/microsoft-store-developer-store-listing-incomplete)

The limit is official documentation. The hidden-error explanation is a
community report quoting Xbox support; do not present every generic forum
suggestion as a Microsoft-confirmed cause. A user's report involving Edge
browser extensions is not automatically applicable to MSIX apps.

## When validation passes but status does not

Reopen/save once and reload. If authorized, try another owner-authenticated
browser session without exporting cookies or clearing unrelated browser data.
Capture the exact error, product/submission IDs, affected language, UTC time,
and a concise redacted screenshot for owner support escalation.

The pilot's first attempted combined add/remove language save failed with
"We are unable to save languages. Please reload the page or try again later."
Reload proved nothing had persisted. Saving the addition and removal as
**separate operations** worked. Do not treat an onscreen Removed row as a
successful deletion until after Save and reload.

Do not endlessly repeat save/reload or delete the entire submission. After a
small bounded set of evidence-based attempts, report what failed and use
[Store developer support](https://aka.ms/storedevsupport) when authorized.
Starting a fresh whole submission is a larger destructive action that needs
separate owner approval and a backup of **all** sections, not just English text.

## Authorized language recreation (last resort)

Only use if the owner requests/approves it, after validating all fields.

1. Back up live product name, all text/empty fields, ordered keywords, captions,
   original screenshot/logo files, hashes and dimensions. Verify the backup
   can be read and files exist. Run the validator before restoring it later.
2. Open **Add/remove languages**. If this is the only listing, Remove may be
   disabled. Don't bypass the disabled UI.
3. Add a temporary appropriate language through **Manage additional languages**
   (English UK was used for the English US pilot), Update, then **Save**.
   Reopen and confirm it exists before touching the original language.
4. Remove only the original language, Save, reopen, and confirm Removed.
   Package-supported languages may remain listed with an **Add** action.
5. Add the original language again, Save and open it. Confirm it is genuinely
   blank. If it retained data, record that instead of claiming recreation.
6. Restore the validated metadata and original artwork. Add feature rows before
   filling them; add keywords with Enter; upload one image at a time; save each
   caption popup with OK. Save the whole listing and reopen.
7. Compare every field, keyword order, caption, screenshot and logo to the
   validated backup. Inspect media previews, not just generic image labels.
8. Remove the temporary language and Save. Confirm only the intended language
   remains. If a mid-operation failure occurs, restore original content and
   clean up the temporary entry before returning, or state the exact blocker.
9. Check overview and publication hold. This procedure must not change packages,
   pricing, ratings, properties, reviewer notes or submit/publish anything.

The pilot successfully executed this procedure and recovered all content. It
**did not** fix the status; shortening the invalid copyright text did.

## Other non-obvious lessons

| Observation | Correct response |
| --- | --- |
| Bundle reports Neutral | Expand included packages; require x64 and ARM64 entries. |
| Future-device-family checkbox changes during analysis | Read and set intended state after processing; verify after save. |
| Zero-price ComboBox temporarily blank | Save, reopen and read persisted numeric zero. |
| IARC Save disabled | Inspect consent gate; owner accepts terms/age declaration. Preserve tab. |
| CmdPal activation appears blank | Wait and inspect main window content; do not capture a blank HWND or PowerDock. |
| Synthetic hotkey ignored | Use `x-cmdpal:`; don't change keyboard settings. |
| Two copies of extension | Verify executing package/version; legacy COM registration can collide. |
| File chooser path loses letters | Separate focus/shortcut/text; prefer literal ValuePattern, then exact file selection. |
| Screenshot upload first unnamed button | It may be trash; inspect context/rectangle and use the plus tile. |
| Unsaved ratings versus saved draft | Record staged/saved separately; navigation may lose the questionnaire. |
| Existing dev UI works | Useful for representative screenshots with provenance, not exact CI release evidence. |
| WACK executable exists | No test evidence until a real run/report; elevation/hardware can be blockers. |

## Skill maintenance

Keep account-specific receipts outside this portable folder. Update the limits
only against current primary documentation. To exercise the helper and metadata
checks without touching Partner Center:

```powershell
& "$skill\tests\StoreBrowser.Tests.ps1"
# Optional: opens a disposable local WinForms test window and temporarily
# takes focus; run only when it will not interrupt the owner's input.
& "$skill\tests\StoreBrowser.Tests.ps1" -ExerciseUi
```

The UI harness never targets the user's browser or clicks Store actions.
