# Edge Favorites for Command Palette

**Draft; reservation and Store ID pending.** Proposed package:
`zadjii.EdgeFavoritesforCommandPalette`. Use the [shared fields](../README.md).

## Description

Find locally stored Microsoft Edge favorites from PowerToys Command Palette and
open them in your browser. Supports the Default profile of supported Edge
Stable, Beta, Dev, and Canary installations. Requires PowerToys Command Palette
and existing local Edge favorites. Does not search browsing history or passwords,
and does not promise access to every Edge profile. Independent community app,
not an official Microsoft extension.

## Features and keywords

Local bookmark search; browser-channel selection; open favorite URLs.
Keywords: bookmarks, favorites, Edge, PowerToys.

## Submission and reviewer notes

Proposed category: Productivity. Logo: `../assets/favorites-store-logo.png`.
Screenshots: a non-personal test bookmark collection.
Privacy: [Edge Favorites](../../../PRIVACY.md#edge-favorites), including remote
browser-channel icons. `runFullTrust` supports desktop COM activation and
read-only access to the user's existing bookmark files.

Test installed/missing channels, nested folders, empty/malformed bookmark files,
Default-profile restriction, search, opening URLs, no modification of bookmarks,
and direct-launch help on both architectures.
