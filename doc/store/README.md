# Separate Microsoft Store apps

## Status and scope

This phase prepares source, assets, listing drafts, and **unsigned submission
bundles** for nine independent Store apps. It does not reserve names, upload
packages, submit for certification, or publish anything. Do not describe a
passing repository preflight as Microsoft certification.

Each row in [`src/store-apps.json`](../../src/store-apps.json) owns a separate
listing, identity, version, and bundle. `TemplateExtension` is a development
scaffold, not a product. There is no umbrella Store listing.

The publisher `CN=5C876BBB-0FB4-4B0F-A120-BE7215903BDF` and display name `zadjii.`
come from the existing Virtual Desktops Store app. The package names are
**proposed** until Partner Center confirms them. `storeId: null` is deliberate:
no product IDs or reservations have been invented.

## Identity gate, before any upload

Reserve each app independently in the intended Partner Center account. Copy its
**Package/Identity/Name**, **Package/Identity/Publisher**, publisher display name,
and Store ID from Product identity. Compare exact values with the catalog and
the app's `Package.appxmanifest`; update both, rebuild, and rerun preflight if
Partner Center assigns a different value. Do not use the display name as an
assumed package identity. Do not ship temporary identities and change them again.

Versions must have four numeric components with the fourth reserved as zero for
Store submissions. Increment versions independently and monotonically per app.
Keep x64 and ARM64 identity, publisher, and version identical.
This transition bumps all nine shipping packages from `0.0.x.0` to `0.1.0.0`;
the unpublished development template keeps its existing version.

The Store identities intentionally differ from the older sideloaded ones.
They are separate installations, not in-place updates. COM class IDs are
preserved; **do not leave the old and new packages installed together** during
normal use because both register the same extension/COM server. Back up settings
first, uninstall the old package, install the Store package, and restart CmdPal.
Vault files and browser bookmarks stay in their original locations; account
sign-ins, API tokens, and local settings may need reconfiguration. No migration
or publisher-bridging scheme is included.

## CI outputs

The `Build Store bundles` workflow runs on PRs, main, tags, and manual dispatch:

1. Validate source identities, activation, artwork, legal documents, and launch
   instructions; build catalog projects separately for x64 and ARM64.
2. Bundle matching packages with MakeAppx. Validate the exact application set,
   both architectures, inner and outer identities/versions, executables,
   self-contained .NET runtime, resource index, asset dimensions, and embedded legal documents.
3. Upload `store-msixbundles` (nine independent bundles) and
   `store-submission-materials` (listing drafts, artwork, policies, catalog).

There is no Azure credential requirement and no active signing/Store publishing
job. The Store supplies distribution signatures. Do not advertise the unsigned
artifacts as user-installable downloads, self-sign them with the former personal
publisher, or distribute them as already-certified apps.

For a local installation smoke test, use a disposable test environment with a
development certificate matching the Store publisher and trust it only there,
or use a Store flight after the first submission permits it. Never install a
test certificate or uninstall an existing extension on a user's machine without
permission. Keep test-signed packages out of submission artifacts.

## Shared listing fields

Use the individual [listing drafts](listings) with these common fields:

| Field | Draft value / action |
| --- | --- |
| Publisher | `zadjii.`; verify account identity |
| Category | Developer tools or Utilities & tools; choose per app |
| Language | English (United States), matching the current UI |
| Price | Free; confirm markets and availability before publishing |
| Website | `https://github.com/zadjii/CmdPalExtensions` |
| Support | `https://github.com/zadjii/CmdPalExtensions/issues` |
| Privacy URL | `https://github.com/zadjii/CmdPalExtensions/blob/main/PRIVACY.md` (must be live after merge) |
| License | MIT for project code; preserve third-party notices; Store terms still apply |
| System requirements | Supported Windows desktop, x64 or ARM64, current PowerToys with Command Palette enabled; PowerToys is installed separately |
| Package capability | `runFullTrust`: desktop COM extension hosted by CmdPal, with the file/network/clipboard behavior disclosed per app |
| Extra media capability | `globalMediaControl`: reads the active Windows media session and sends explicit playback controls |

All packages currently retain `internetClient`; this declaration does not mean
every extension transmits data. See the privacy policy for actual behavior.
Desktop/full-trust apps are not sandboxed like ordinary UWP apps. Explain
restricted capabilities in certification notes; acceptance is Microsoft's
decision, not something the CI check can grant.

Each app includes offline LICENSE, PRIVACY.md, and THIRD-PARTY-NOTICES.md.
Packages are self-contained and retain assets/legal files as loose package
content, rather than hiding them inside a single-file executable.

## Artwork

Reuse the existing extension artwork where available. `iconSource` in the app
catalog selects the original file; generation fails if that file is missing or
invalid, rather than silently substituting a different design.

| Extension | Package and listing artwork | CmdPal artwork |
| --- | --- | --- |
| Obsidian Notes | Existing `Assets\obsidian-logo.png` | Same original logo |
| TMDB Search | Existing `Assets\Tmdb-312x276-logo.png` | Same original logo, also retained in About |
| Segoe Icons | Existing `Assets\WinUI3Gallery.png` | Same original icon |
| Hacker News | Replacement for generic template images | Original Hacker News site favicon |
| Mastodon | Replacement for corrupt template images | Existing remotely loaded Mastodon icon, unchanged |
| Edge Favorites | Replacement for corrupt template images | Existing browser-channel icons, unchanged |
| NFL Scores | Replacement for corrupt template images | Existing football glyph and team logos, unchanged |
| Media Controls | Replacement for generic template images | Existing play/pause/track glyphs, unchanged |
| SpongeBot | Replacement for cartoon artwork with no recorded redistribution permission | Matching replacement, not the remote meme |

Existing source images are not overwritten or redrawn. Scale variants retain
their colors, transparency, and aspect ratio, centered on transparent canvases;
light/unplated variants do not recolor branded artwork. The supplied raster
sources are reused as-is (Obsidian 512x512, TMDB 185x133 despite its filename,
WinUI Gallery 108x108); upscaling does not create additional detail.

Generated replacement artwork also keeps its colored background and white
symbol in both target-size variants used by Start and the taskbar. "Unplated"
means Windows should not add its own tile background, not that the image must
be monochrome; transparent padding remains outside the colorful artwork.

Outputs include 100/200/400 scale square, wide, splash, and Store images,
unplated/light-unplated target-size icons, and 300x300 listing logos in `assets`.
Regenerate with `Generate-StoreAssets.py`, or use `--check` to compare output.
`tests\StoreAssets.Tests.py` checks source selection, proportional resizing,
color preservation, and missing-source failures. Pillow is required for those
Python commands, not normal MSIX builds. Package preflight verifies both the
generated icons and included runtime source icons against the files on disk.

## Gates that require the later submission phase

- Confirm each name reservation, identity, Store ID, account/publisher eligibility,
  pricing, markets, and contact/support information.
- Capture **real** current screenshots inside CmdPal; no mockups or placeholder
  screenshots are supplied. Use non-sensitive test data and meet the current
  Partner Center image requirements.
- Complete each age-rating questionnaire truthfully. Hacker News, Mastodon, and
  external content can include mature or user-generated material. Do not
  auto-answer all products as suitable for every age.
- Verify trademark/name permissions and external API/data rights. In particular,
  TMDB/JustWatch attribution and approved branding, ESPN sports data/team marks,
  and Mastodon/Obsidian naming must meet their owners' current terms. Reusing
  existing artwork does not establish trademark permission or grant new rights
  to redistribute third-party service content.
- Run the Windows App Certification Kit and actual installed CmdPal smoke tests
  on x64 **and ARM64**. Compilation and bundle inspection do not test ARM64
  execution, host discovery, authentication, network failure UX, or Store signing.
- For every app: install/discover, direct-launch help, core actions, settings or
  credentials, restricted capabilities, offline/empty state, uninstall/reinstall,
  and upgrade from an earlier Store version. Use the individual draft's cases.
- Review public policies/links and certification notes; the owner approves the
  submission and publication settings. Retain artifact/run/commit/version IDs.

The first pilot is [Hacker News](hackernews-submission-plan.md), after this work
merges. Do not hold out the other eight apps as submitted or approved.

## Later automation

The Store CLI/submission API can update existing MSIX apps, but does not create
the initial listings/reservations. Finish the first submission and ratings in
Partner Center, then consider a separate, reviewed submission workflow with
per-app product IDs and approval gates. Partner Center's Entra app association
and permissions are distinct from the deprecated Azure signing app; do not
reuse its secrets assuming it grants Store access.

References:

- [Store package requirements](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/app-package-requirements)
- [Product identity details](https://learn.microsoft.com/en-us/windows/apps/publish/view-app-identity-details)
- [Store Developer CLI](https://learn.microsoft.com/en-us/windows/apps/publish/msstore-dev-cli/overview)
- [CLI commands and limitations](https://learn.microsoft.com/en-us/windows/apps/publish/msstore-dev-cli/commands)
- [MSIX submission API](https://learn.microsoft.com/en-us/windows/uwp/monetize/create-and-manage-submissions-using-windows-store-services)
