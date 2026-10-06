# Hacker News for Command Palette

**Draft; reservation and Store ID pending.** Proposed package:
`zadjii.HackerNewsforCommandPalette`. Use the [shared submission fields](../README.md).

## Description

Browse top Hacker News stories directly in PowerToys Command Palette. See story
titles, scores, comment counts, and authors, then open the article or discussion
in your browser. Requires a separate installation of PowerToys with Command
Palette enabled and an internet connection. No Hacker News sign-in is required.
This is an independent extension, not an official or endorsed Hacker News app.

## Features and keywords

Top stories; article and comment links; story metadata. Keywords: news,
Hacker News, PowerToys, Command Palette.

## Submission and reviewer notes

Proposed category: News & weather. Logo: `../assets/hackernews-store-logo.png`.
Screenshots needed: real story list and article/comment context menu.
Privacy: [Hacker News section](../../../PRIVACY.md#hacker-news), including
preloaded article-host favicons. Age ratings must consider linked external
news/user content. No account or reviewer credentials.

Install current PowerToys, enable CmdPal, install the package, restart CmdPal,
press `Win+Alt+Space`, and select Hacker News. Direct app launch displays console
instructions. `runFullTrust` enables the desktop COM extension, not elevation.
Test top stories, both link and text posts, comments, refresh, loss of network,
empty/error response, and clean shutdown on x64 and ARM64. See the
[pilot plan](../hackernews-submission-plan.md) before proceeding.
