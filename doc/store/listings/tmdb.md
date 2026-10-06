# TMDB Search for Command Palette

**Draft; reservation and Store ID pending.** Proposed package:
`zadjii.TMDBSearchforCommandPalette`. Use the [shared fields](../README.md).

## Description

Search movies and inspect details or available streaming providers in PowerToys
Command Palette. Requires internet access, PowerToys Command Palette, and your
own TMDB API Read Access Token configured in the extension.
Choose the language of movie results and details in the extension settings.
This product uses the TMDB API but is not endorsed or certified by TMDB.
Watch-provider availability is supplied through TMDB and powered by JustWatch.
No movie playback or subscription is included.

## Features and keywords

Movie search; details and posters; watch-provider links.
Keywords: movies, TMDB, JustWatch, PowerToys.

## Submission and reviewer notes

Proposed category: Entertainment. Logo: `../assets/tmdb-store-logo.png`.
Screenshots: approved test search and provider details; verify artwork rights.
Privacy: [TMDB Search](../../../PRIVACY.md#tmdb-search). API token stored in
Credential Locker. `runFullTrust` supports desktop COM activation and credential
access. Confirm TMDB/JustWatch's current API, attribution, and approved-logo
requirements before submission. The existing TMDB logo is reused for package
and listing variants with its original proportions and colors. A package logo
is not a substitute for service-required in-product attribution. The command's **About and data
attribution** context action provides the TMDB notice, its existing logo,
JustWatch credit, and privacy/support links, both before and after token setup.

Test unset/invalid/valid tokens, search with no results, result-language changes,
details, provider links,
offline behavior, token reconfiguration, and launch help on x64 and ARM64.
Check the About page and provider attribution in the actual host UI.
Arrange reviewer access according to API terms without committing a token.
