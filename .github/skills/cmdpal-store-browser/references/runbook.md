# Store draft runbook

UI labels and requirements can change. These observations were last checked
2026-10-07. Re-read the linked Microsoft documentation if current UI conflicts.

## 1. Gather inputs and inspect the extension

Start with the manifest, project/build configuration, command provider/pages,
settings/authentication/storage code, package assets, LICENSE, privacy policy,
third-party notices, and any existing listing draft.

Do not infer behavior solely from capabilities. Record actual reads/writes,
clipboard use, accounts, credential storage, network services, remote icons,
tracking/telemetry, browser launches, user content and age-sensitive content.
For example, a favicon request may disclose an IP address to an article host
before the user opens the article.

Confirm:

- An independent manifest identity and bundle, not a suite or template project.
- Same identity, publisher, display publisher, four-part version and minimum OS
  across x64 and ARM64. Fourth version component is zero for Store submission;
  versions increase monotonically. Do not assume every new app is `0.1.0.0`.
- Desktop app-extension/COM registration matches the executable and actual
  CmdPal provider. Direct executable launch should explain PowerToys/CmdPal
  usage rather than appear broken or leave an unexplained console.
- Either self-contained runtime files or correctly declared dependencies;
  actual payload contains required assets, resource index and offline legal
  documents. Do not require this skill's pilot project layout.
- Existing artwork is reused and proportionally scaled. Check small/large
  square, StoreLogo, tile/splash assets referenced by the manifest and suitable
  Start/taskbar target-size variants. Colorful unplated icons need not become
  monochrome. Preserve original art; missing/corrupt/template artwork and
  missing redistribution rights are separate problems.
- Public website, support, privacy and license links resolve on the released
  branch without requiring the reviewer to sign in.

Run existing repository checks/builds before adding any tooling. Document
missing checks rather than treating a pass from another project as evidence.
Unsigned MSIX bundles can be Store submission inputs; Store distribution
signatures are supplied by Microsoft. Do not resurrect personal/Azure signing
or alter publisher identity to make a local installation succeed.

## 2. Account, product and identity

Navigate visibly to Partner Center, Apps and games. Verify the intended
developer/publisher account. If necessary read Product identity of an existing
app belonging to that account, without editing it. Being signed in to Azure,
GitHub or another browser profile does not establish Partner Center access.

With owner authorization, choose New product / **MSIX or PWA app** (wording may
vary), not the unpackaged EXE/MSI submission path. Reserve one appropriate app
name. Stop for enrollment, MFA, legal or payment screens; the owner handles
them. Record any reservation expiration notice.

Open **Product identity** and copy exact values:

| Identity data | Compare against |
| --- | --- |
| Package/Identity/Name | Manifest Identity Name and inner packages |
| Package/Identity/Publisher | Manifest Publisher, not a remembered old certificate subject |
| Package/Properties/PublisherDisplayName | Manifest PublisherDisplayName |
| Store ID | Product receipt/catalog; never fabricate a value |
| Package family name | Observed PFN; do not compute it from guessed values |

A display name is not a package name. If any identity differs, stop package
upload, correct source through the project's normal review process, rebuild
both architectures, and get a new artifact/hash.

Store identity changes can make old and new packages separate installations.
Preserved COM class IDs may collide when both remain installed. Back up settings
and get permission for removal/migration; never silently uninstall the user's
working extension.

## 3. Provenance and draft creation

Use the intended approved source revision (merged main in the pilot), not an
unrelated latest PR artifact. On GitHub, an example using project-specific
values is:

```powershell
gh run view $runId --repo $repository --json headSha,conclusion,url
gh run download $runId --repo $repository --name $artifactName --dir $downloadDirectory
Get-FileHash -LiteralPath $bundlePath -Algorithm SHA256
```

Confirm successful workflow, commit, exact app/version, both architectures and
full bundle contents using the project's packaging checks/Windows SDK tooling.
The bundle filename alone is not evidence. An artifact archive may contain
many apps; upload **only the selected app's bundle**. Keep the original unsigned
submission file unchanged and separate from any test-signed copy.

Create/open a submission draft. Record the actual URL and ID; use observed
links instead of inventing routes or numeric language IDs.

First set **Submission options -> Don't publish this submission until I select
Publish now**. Save and reopen; check the radio's selected state. A pricing
schedule of "as soon as possible" is not a substitute for this publication hold.

## 4. Properties, privacy and price

Choose the category based on the app (news, utilities, developer tools, etc.).
Provide public privacy policy, website and support links in their actual fields.
Make the personal-information declaration match real network/account/content
behavior and the portal's current definition. Do not mechanically reuse "Yes"
or "No" from Hacker News. Do not enable accessibility, AI, Mixed Reality or
regulatory declarations without evidence.

Confirm price and markets with the owner. Free requires an explicit zero price
when the UI has not initialized it. Select the currency and zero retail price,
close the dropdown, save, reopen and read the persisted value. In the pilot the
UI briefly displayed an empty value after selecting zero; the reopened page
showed `0`. A Complete badge alone cannot verify pricing.

Record public/private audience, discoverability, selected markets, future-market
opt-in, release schedule and trials. Worldwide/future-market defaults are not
automatically correct for another app's content/data rights. Do not make
unsupported regulatory or regional-rights affirmations.

## 5. Package upload

Open Packages, browse to the **exact absolute file path**, select/open the file
and wait. File selection, upload, analysis and successful validation are
different stages. Read errors/warnings, not just a filename or enabled Save.

Expand **Show details**:

- Outer bundle may be reported as **Neutral**; this is normal for a bundle.
  The included packages must explicitly show **Arm64** and **X64**.
- Check both package versions, identity, languages, Windows.Desktop minimum OS,
  size and declared capabilities against the manifest/artifact.
- Enable Windows 10/11 Desktop only for a desktop CmdPal extension; leave
  Mobile, Xbox, Team and Mixed Reality off unless genuinely supported.
  Verify the future-device-family option explicitly; package processing may
  change defaults. Do not leave it on accidentally.
- Explain restricted capabilities when prompted. `runFullTrust` enables the
  desktop process/COM server; it does not mean the extension requires elevation.
  Additional capabilities need app-specific reasons.

Save; navigate away and reopen. Verify the package remains, targeting is saved,
and the overview reports validation/section completion. None of this is WACK
or runtime testing.

## 6. Listing text and artwork

Complete the package-supported language (English US in the pilot). Explicitly
select a reserved product name if required. Do not create translations or
languages that the owner did not request, except a reversible temporary entry
for an authorized recovery procedure.

Describe the real purpose, supported features, limitations, external services
and separately installed PowerToys requirement. State how to restart/open
CmdPal and find this extension's command or fallback action. Default shortcut
is `Win+Alt+Space`; the user may have changed it. Explain direct launch, account
requirements and third-party non-endorsement where appropriate.

| Field | Limit / rule |
| --- | --- |
| Description | Required, up to 10,000 characters; plain text, no HTML |
| What's new | Up to 1,500; blank for the first submission |
| Product features | Up to 20, each up to 200; no manual bullet prefix |
| Short title | Optional, up to 50 |
| Voice title | Optional, up to 255 |
| Short description | Up to 1,000; 270 or fewer recommended |
| Keywords | Up to 7, each up to 40; at most 21 words total |
| **Copyright and trademark info** | **200 maximum, even if UI displays no error** |
| Additional license terms | Up to 10,000; leave blank unless owner provides different terms |
| Developed by | Up to 255; not the account publisher field |
| Screenshot captions | Up to 200 each |

Blank optional fields are legitimate. Do not add fabricated release notes,
legal amendments or extra images just to make every input nonempty.
Keep full legal notices in the package/public documents, not in an overlong
copyright field. A short app-specific non-endorsement/rights/license notice is
usually enough here; count it before entering.

Desktop screenshots: genuine PNG captures, at least 1366x768 for landscape
(swap dimensions for portrait), under 50 MB. One is required; several useful
views are better than duplicates. Current UI may recommend four and allow more
uploads than older docs; verify current limits. A 300x300 Store tile is **not**
a screenshot. Use a PNG tile under 5 MB to satisfy the observed upload UI.
Xbox art is not required for a Desktop-only app.

Capture actual extension content in CmdPal, not a mockup or a browser showing
the linked website. Show primary results plus a relevant action/settings view.
Use safe non-personal data; inspect every image before upload. Crop unrelated
desktop pixels without altering product content. Prefer resizing the actual
window to meet dimensions over upscaling a small capture.

Use original, approved package artwork for the Store tile. Do not replace it
with an unrelated service logo or treat existing use as proof of rights.
Upload images one at a time, wait for the count/preview, add truthful captions,
save, reload and verify. Retain original files and hashes for recovery.

## 7. Age ratings and owner agreements

Read every question and record the answer **with its factual rationale**.
Separate bundled content from internet content, textual references from visual
depictions, native user interaction from opening a browser, and unfiltered
content from the app's primary purpose. Consider violence, sexual/drug
references, profanity, user-generated material, purchases, location sharing,
age-restricted goods, gambling/rewards and browser/search functionality.

Do not infer "Everyone" because no sign-in is required. A news extension can
expose unfiltered mature titles/content. Conversely, do not label a file,
clipboard or media-control utility as a social network merely because the HN
pilot used online content. No universal answer sheet is supplied deliberately.

Generate and review the IARC preview; ratings differ by authority/territory.
The owner must accept **IARC Terms of Use** and affirm legal majority.
Never toggle that checkbox through automation. If unavailable, preserve the
preview tab, work elsewhere, and report **staged but not saved**. After the
owner saves, observe Complete without pretending the agent accepted terms;
re-read final ratings if they are needed in the receipt.

## 8. Reviewer notes and testing

Use Additional Testing Information or the currently exposed reviewer-notes
surface. Example outline, adapted to actual behavior:

```text
This is a desktop extension for Microsoft PowerToys Command Palette.
Install [supported PowerToys version], enable Command Palette, install this
package, restart Command Palette, and invoke [exact command/fallback].
Open CmdPal with its configured shortcut (default Win+Alt+Space).
Direct executable launch displays setup instructions, not a standalone UI.
Exercise [specific core actions and expected results].
[No account required / test account handoff through an approved secure channel.]
runFullTrust is required for the desktop COM extension; elevation is not required.
[Other capabilities and concrete file/network/clipboard/media behavior.]
[Known legacy identity/COM collision and approved clean-environment setup.]
```

Do not put passwords/tokens into Git or this skill's JSON receipt.

Test the actual uploaded version in an approved disposable environment:
install/discover, direct-launch help, core actions, settings/authentication,
offline/empty/error states, restart, uninstall/reinstall, and upgrade where
applicable. Test **both x64 and ARM64**. Record installed full name, version,
architecture, executable source and report paths.

WACK is normally in `C:\Program Files (x86)\Windows Kits\10\App Certification Kit`.
Follow its current instructions for elevation and an active user session.
In an authorized test environment, the documented installed-package form is:

```powershell
& $appcert test -packagefullname $installedPackageFullName -reportoutputpath $reportPath
```

WACK can launch/exercise the app; package-path testing can affect deployment.
Do not run `reset`, install certificates, replace/uninstall packages or accept
UAC on the owner's machine without permission. Help output is not a test run.
Never claim ARM64 runtime coverage from an x64 build or static ZIP inspection.
If screenshots use an existing development build, record that provenance and
require owner review of representativeness; it is not release-package testing.

## 9. Final readback and handoff

Reopen Properties, Pricing, Packages, each language listing, Age ratings,
reviewer notes and Submission options. Compare saved text and images to the
receipt, not just checkmarks. Preserve the manual hold. Record the app as draft,
not published, until an actual separately authorized action changes that.

Before an explicit certification request, surface unresolved rights, owner
consents, exact-package/WACK/hardware testing and any portal error. Approval
to submit is distinct from approval to publish. A successful Store submission
or certification still needs verification of signed Store installations before
advertising a release. Do not expand this pilot to other apps automatically.

## Primary references

- [Product identity](https://learn.microsoft.com/en-us/windows/apps/publish/view-app-identity-details)
- [Package requirements](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/app-package-requirements)
- [Listing text and languages](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/add-and-edit-store-listing-info)
- [Copyright/keywords/developer limits](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/add-additional-information)
- [Screenshots, captions and Store art](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/screenshots-and-images)
- [Age ratings](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/age-ratings)
- [WACK](https://learn.microsoft.com/en-us/windows/uwp/debug-test-perf/windows-app-certification-kit)
- [Store CLI commands](https://learn.microsoft.com/en-us/windows/apps/publish/msstore-dev-cli/commands)

Later API/CLI update automation is a separate task requiring account association,
per-app IDs and approval gates. Do not assume a GitHub/Azure signing credential
grants Partner Center access or that update APIs replace initial onboarding.
