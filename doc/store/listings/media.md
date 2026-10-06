# Media Controls for Command Palette

**Draft; reservation and Store ID pending.** Proposed package:
`zadjii.MediaControlsforCommandPalette`. Use the [shared fields](../README.md).

## Description

See track details and control the active Windows media session from PowerToys
Command Palette. Play or pause and request the previous or next track in a
compatible media app. Requires PowerToys Command Palette and a media player
that exposes Windows media controls. No player, music catalog, or streaming
subscription is included.

## Features and keywords

Track and artist display; play/pause; previous/next; active-session changes.
Keywords: media, playback, music, PowerToys.

## Submission and reviewer notes

Proposed category: Utilities & tools. Logo: `../assets/media-store-logo.png`.
Screenshots: a permitted test track and the empty-session state.
Privacy: [Media Controls](../../../PRIVACY.md#media-controls).
`runFullTrust` supports desktop COM activation. `globalMediaControl` is needed
to read Windows media sessions and request explicit playback actions.

Test startup with no player, start/stop playback after extension launch, switch
between players, track changes, play/pause/previous/next, unsupported commands,
player exit, extension shutdown, and direct-launch guidance on x64 and ARM64.
The legacy release was marked broken; the new empty/session-change handling
must receive actual host/device testing before this app is submitted.
