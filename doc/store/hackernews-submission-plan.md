# Deferred Hacker News Store pilot

**Plan only. Start after the preparation changes have merged and the owner
requests the computer-use phase. No Store action has been performed here.**

1. Open the owner's signed-in Partner Center account using the available
   computer-use/browser tools. Confirm the intended publisher/account. The owner
   handles credentials, MFA, enrollment fees, tax/payment details, and agreements;
   do not record credentials or bypass interactive security prompts.
2. Reserve a **separate Hacker News app**, not a suite. Use the listing draft's
   name only if available and permissible. Read Product identity and record the
   assigned package name, publisher, publisher display name, and Store ID.
   Compare with `src/store-apps.json` and the HN manifest. Stop before uploading
   on any mismatch; make a reviewed identity correction and rebuild first.
3. Obtain `store-msixbundles` from a successful run of the merged commit.
   Record workflow run, commit, package version, and bundle SHA-256. Select only
   `zadjii.HackerNewsforCommandPalette_<version>_x64_ARM64.msixbundle` (or the
   corrected reserved identity); never upload another extension's bundle.
4. Perform a permitted installed-package test in a disposable environment.
   Confirm x64 and ARM64 host discovery, 25 top stories, articles and comment
   links, non-link stories, external favicons, no-network behavior, direct launch
   console guidance, and uninstall. Run WACK. Capture genuine CmdPal screenshots
   without personal data; upload the original listing logo, not an HN logo.
5. Create the first submission in Partner Center. Confirm free pricing, markets,
   category, supported desktop device families, language, and availability with
   the owner. Complete age/content ratings based on internet news and linked
   user-generated material, not an assumed child-friendly rating.
6. Upload the one bundle. Resolve Partner Center package-validation errors;
   verify both x64 and ARM64, identity, minimum OS, version, and `runFullTrust`
   explanation. Do not defeat signature/identity checks or silently change the
   manifest to fit an unrelated app.
7. Enter the [HN listing](listings/hackernews.md), screenshots, support/website
   links, and the public privacy URL. Confirm policy/legal links resolve after
   merge. Explain PowerToys installation and `Win+Alt+Space`, external favicon
   requests, independent/non-endorsed status, and no required sign-in.
8. Add certification notes: this is a desktop COM extension for PowerToys
   Command Palette, not a standalone reader; direct launch only shows
   instructions. Include exact install/discovery/test steps and the reason for
   `runFullTrust`. No test credentials are needed.
9. Show the owner a final review: selected product, bundle/version/hash,
   architecture support, screenshots, listing, ratings, markets, capability
   justification, and publication timing. **Obtain explicit approval before
   final certification submission or publication**; prefer manual publication
   after certification unless the owner chooses otherwise.
10. Monitor certification and address concrete feedback in new builds. After
    approval and an authorized publish, verify the Store URL and signed
    installations on both architectures. Replace only HN's `storeId: null` with
    the real ID and add its Store link. Keep other apps explicitly pending.

After the pilot, repeat the identity/listing/rating/test process separately for
each extension. Only then design update automation using Partner Center
credentials, per-app versioning, an approval environment, and no implicit
publish-on-merge behavior.
