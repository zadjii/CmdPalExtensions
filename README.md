# Mike's Command Palette Extensions

A collection of independent extensions for [PowerToys Command Palette](https://aka.ms/powertoys).
Contributions are welcome.

## Extensions

Each extension is its own app/package, not one combined installation.

| Extension | Description | Store listing draft |
| --- | --- | --- |
| Hacker News | Browse top stories and open articles/discussions | [Details](doc/store/listings/hackernews.md) |
| Obsidian Notes | Search, preview, edit, and open notes in a local vault | [Details](doc/store/listings/obsidian.md) |
| Mastodon | Browse your chosen Mastodon instance and interact with posts | [Details](doc/store/listings/mastodon.md) |
| TMDB Search | Search movies and streaming providers with your TMDB API token | [Details](doc/store/listings/tmdb.md) |
| Segoe Icons | Find and copy icon characters or XAML | [Details](doc/store/listings/icons.md) |
| Edge Favorites | Search favorites in supported Edge Default profiles | [Details](doc/store/listings/favorites.md) |
| NFL Scores | View ESPN game scores and details | [Details](doc/store/listings/nfl.md) |
| SpongeBot | Convert text to alternating case and copy it | [Details](doc/store/listings/sponge.md) |
| Media Controls | Control the active Windows media session | [Details](doc/store/listings/media.md) |

**Store publishing is being prepared; these are not live Store listings.**
[Historical GitHub releases](https://github.com/zadjii/CmdPalExtensions/releases)
remain available as legacy, x64-only sideload packages. New CI artifacts are
unsigned Store submissions, **not sideload installers**.

After installing a published extension, enable Command Palette in PowerToys and
open it with `Win+Alt+Space` (or your configured shortcut). Starting an extension
directly displays a short console explanation instead of opening a standalone
app. SpongeBot appears as a fallback action for typed text.

## Package builds

The **Build Store bundles** workflow builds all nine shipping extensions for
x64 and ARM64 and produces **nine separate `.msixbundle` files**, each containing
both architectures. `src/store-apps.json` is the explicit application catalog;
the development template is excluded. Packages include their .NET runtime, so
users do not need to install a separate developer runtime. Missing apps/architectures, duplicates,
identity/version mismatches, and missing packaged artwork/legal documents fail
validation. The `store-msixbundles` artifact is a download archive containing the
independent bundles, not a tenth combined app.

Store submissions do not need our former public-CA signature. Microsoft Store
signs packages for distribution after approval. CI has no Azure authentication,
signing job, Store upload, or automatic publishing step.

On Windows, with the .NET 9 SDK and Windows SDK installed, run from the repo root:

```powershell
.\src\tools\tests\StoreReadiness.Tests.ps1
.\src\tools\tests\MsixPackaging.Tests.ps1
.\src\tools\tests\MediaControls.Tests.ps1
.\src\tools\Build-StorePackages.ps1 -Platform x64
.\src\tools\Build-StorePackages.ps1 -Platform ARM64
.\src\tools\New-MsixBundles.ps1
.\src\tools\Test-StoreReadiness.ps1 -BundlesPath .\artifacts\bundles
```

Use fresh `artifacts\packages` and `artifacts\bundles` directories; scripts reject
stale output rather than mixing old identities into submissions. `global.json`
selects .NET 9, and `.github\nuget.config` uses public NuGet packages instead of the
optional local SDK feed in the root configuration.

Package and 300x300 Store listing icons are checked in. The existing Obsidian,
TMDB, and WinUI Gallery artwork is reused without changing its colors or aspect
ratio; replacements are limited to template/corrupt assets and SpongeBot's
cartoon artwork, which has no recorded redistribution permission. See the
[artwork inventory](doc/store/README.md#artwork).
To regenerate, use Python with Pillow and run
`python .\src\tools\Generate-StoreAssets.py`; `--check` compares the committed
artwork with the generator output. Run
`python .\src\tools\tests\StoreAssets.Tests.py` for scaling regression tests.

See [Store preparation and release gates](doc/store/README.md) and the
[deferred Hacker News submission plan](doc/store/hackernews-submission-plan.md).
The proposed identities must be matched to Partner Center reservations before
uploading. Switching from the old sideload identities is a one-time separate
installation; automatic settings/credential migration is not implemented.

## Deprecated signing notes

The old Azure Trusted Signing and x64 package-collection path is retired.
`Find-Msixs*.ps1` and `Sign-Msixs.ps1` have been removed. The
[signing notebook](doc/signing-packages.ipynb) remains only as historical personal
notes; do not use it for these Store identities. Existing Azure/GitHub signing
resources have not been deleted and are no longer used by this workflow.

## License, privacy, and support

[MIT license](LICENSE) · [Privacy policy](PRIVACY.md) ·
[Third-party notices](THIRD-PARTY-NOTICES.md) ·
[Support/issues](https://github.com/zadjii/CmdPalExtensions/issues)

## Mastodon home instance

Open the Mastodon extension's settings in Command Palette (also available from
the login, explore, and home commands) and set **Home instance** to your server's
hostname or HTTPS URL, such as `fosstodon.org` or `https://fosstodon.org`.
Use the server's base URL, without a path or query.

Login, Explore, your home timeline, replies, favorites, and boosts all use the
selected instance. Changing instances refreshes the timelines and switches to
that instance's saved login, or asks you to sign in if you have not used it before.
Credentials are stored separately for each instance; existing mastodon.social
logins continue to work. Logging out only removes the selected instance's login.

Run the Mastodon instance regression tests with
`dotnet test .\src\extensions\MastodonExtension.Tests\MastodonExtension.Tests.csproj -p:Platform=x64 -p:RestoreConfigFile="$PWD\.github\nuget.config"`.

## TMDB Search setup

Sign in to [TMDB's API settings](https://www.themoviedb.org/settings/api) and
copy your **API Read Access Token** into the extension's login form. Use the
read access token, not your TMDB password or API key. Signing in to the TMDB
website alone does not authenticate the extension. The token is stored in
Windows Credential Manager. Use **Update TMDB API token** in the search command's
context menu to replace it.

Open the extension's settings from Command Palette settings or the command's
context menu to set **Language**. Both movie searches and movie details use this
language code (for example, `fr`, `it`, or `en-US`). The default is `en-US`;
leaving the setting blank also uses `en-US`. This changes the language of
returned titles, descriptions, and genres, not the movies' original language
or the streaming-provider region (currently US).

Authentication, network, and API errors are shown separately from an empty
search result. If authentication fails, open the error item to enter your
API Read Access Token again.

## Contributing

Contributions and regression tests are welcome.

Run the TMDB regression tests on Windows with the .NET 9 SDK or later:

```powershell
dotnet test .\src\extensions\TmdbExtension.Tests\TmdbExtension.Tests.csproj -p:Platform=x64 -p:RestoreConfigFile="$PWD\.github\nuget.config"
```

These tests use simulated HTTP responses; no TMDB account or API token is required.
