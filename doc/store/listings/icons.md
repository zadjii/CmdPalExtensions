# Segoe Icons for Command Palette

**Draft; reservation and Store ID pending.** Proposed package:
`zadjii.SegoeIconsforCommandPalette`. Use the [shared fields](../README.md).

## Description

Browse the Segoe Fluent icon list in PowerToys Command Palette and copy a
character or XAML snippet for your work. Requires PowerToys Command Palette
and the applicable Windows Segoe font. This extension does not distribute a
font file and is not an official Microsoft product.

## Features and keywords

Local icon search; glyph preview; character and XAML copy; documentation links.
Keywords: icons, Segoe, XAML, developer tools.

## Submission and reviewer notes

Proposed category: Developer tools. Logo: `../assets/icons-store-logo.png`.
Screenshots: search and copy context menu. Privacy:
[Segoe Icons](../../../PRIVACY.md#segoe-icons). MIT attribution for icon metadata
derived from WinUI Gallery is retained. The existing `WinUI3Gallery.png` icon
is reused for the package, listing, and runtime UI; confirm applicable branding
terms before submission. `runFullTrust` supports desktop COM
activation; copy actions use the Windows clipboard.

Test offline search, glyph rendering, character/XAML copy, documentation and
optional WinUI Gallery links, and direct-launch instructions on both
architectures. Confirm the packaged `Assets\icons.json` is available.
