# Third-party notices

Repository code is licensed under the root MIT LICENSE, including existing
Microsoft copyright notices. Geometric replacement icons are original project
artwork under that license. Resized copies of the existing Obsidian, TMDB, and
WinUI Gallery artwork retain their upstream rights and are not newly licensed
under MIT merely because this project scales or packages them.

## Redistributed software and data

| Component | License / upstream |
| --- | --- |
| Microsoft Command Palette extension SDK and toolkit | [MIT, PowerToys](https://github.com/microsoft/PowerToys/blob/main/LICENSE) |
| .NET runtime and Microsoft.Extensions libraries | [MIT, dotnet/runtime](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT); runtime components also carry [third-party notices](https://github.com/dotnet/runtime/blob/main/THIRD-PARTY-NOTICES.TXT) |
| C#/WinRT and WinRT.Runtime | [MIT](https://github.com/microsoft/CsWinRT/blob/master/LICENSE) |
| Shmuelie.WinRTServer | MIT, copyright (c) 2025 Shmueli Englard |
| Html Agility Pack (Mastodon and NFL) | [MIT](https://github.com/zzzprojects/html-agility-pack/blob/master/LICENSE), copyright ZZZ Projects Inc. |
| RestSharp (Mastodon and TMDB) | [Apache License 2.0](https://github.com/restsharp/RestSharp/blob/dev/LICENSE.txt) |
| Segoe icon name/code-point data derived from WinUI Gallery | [MIT](https://github.com/microsoft/WinUI-Gallery/blob/main/LICENSE), copyright Microsoft Corporation |

The source headers and upstream license/notice files must be retained. Installed
Segoe fonts are used through Windows; no font file is redistributed by this
project. Build-only analyzers and SDK tools are not part of the app's licensing
grant. NuGet package metadata and embedded notices are authoritative for the
particular dependency versions in a build.

Full license and runtime notice texts are included in `ThirdPartyLicenses` in
each package (source copies: [`doc/licenses`](doc/licenses)). MIT components
retain these copyright attributions: Microsoft Corporation; .NET Foundation and
contributors; Shmueli Englard (2025); ZZZ Projects Inc. RestSharp is distributed
under Apache License 2.0; its source and history are at
https://github.com/restsharp/RestSharp.

## Services, content, and trademarks

These are independent community extensions, not official clients endorsed by
Microsoft, Y Combinator/Hacker News, Obsidian, Mastodon, TMDB, JustWatch, the NFL,
ESPN, or their affiliates. Third-party names identify compatibility or data
sources, not sponsorship. Third-party content, logos, and trademarks are not
relicensed under this repository's MIT license.

**TMDB:** This product uses the TMDB API but is not endorsed or certified by
TMDB. Movie information and artwork come from TMDB. Streaming availability is
provided through TMDB's watch-provider data and attributed to **JustWatch**;
follow the supplied watch-provider link. API users and distributors must comply
with current TMDB and JustWatch terms, attribution, and approved-logo rules.

**NFL Scores:** Game data and team logos are provided by ESPN. No right to
redistribute broadcasts, league marks, or sports data beyond the applicable
service terms is granted here. Confirm distribution permission before submitting
this app.

**Mastodon and Hacker News:** Posts, linked articles, user images, and site
favicons belong to their respective authors and hosts.

**Existing app artwork:** The Obsidian logo (`obsidian-logo.png`), TMDB logo
(`Tmdb-312x276-logo.png`), and WinUI Gallery icon (`WinUI3Gallery.png`) are reused
from the repository, including proportionally resized package/listing variants.
Their respective owners retain copyright and trademark rights. This preparation
does not assert endorsement or new redistribution permission; confirm applicable
branding terms before Store submission.

**Edge Favorites:** Microsoft Edge is a Microsoft trademark. Browser-channel
icons currently fetched from Wikimedia Commons remain subject to their original
rights and source-page terms.

Historical, unused image files in source control (including old template and
SpongeBot artwork) are not part of the new `Assets\Package` output. Do not add
them back to release packages without checking their rights.
