# Obsidian Notes for Command Palette

**Draft; reservation and Store ID pending.** Proposed package:
`zadjii.ObsidianNotesforCommandPalette`. Use the [shared fields](../README.md).

## Description

Find and preview Markdown notes from your local Obsidian vault in PowerToys
Command Palette. Open notes in Obsidian or explicitly append and save changes
from the palette. Choose a vault folder in the extension's settings.
Requires PowerToys Command Palette; opening Obsidian links also requires
Obsidian installed. This is an independent, unofficial extension.

## Features and keywords

Local note search; Markdown preview; note editing; Obsidian links.
Keywords: notes, Markdown, Obsidian, PowerToys.

## Submission and reviewer notes

Proposed category: Productivity. Logo: `../assets/obsidian-store-logo.png`.
Screenshots: test vault search and preview without private notes.
Privacy: [Obsidian Notes](../../../PRIVACY.md#obsidian-notes). `runFullTrust`
supports COM activation and user-selected vault access. No service account
required. The existing Obsidian logo is reused in the package and runtime UI;
confirm logo and compatibility-name permissions before submission.

In CmdPal, select the Obsidian command and configure a disposable vault.
Test missing/empty vaults, recursive search, preview, Unicode file names,
explicit save/append, Obsidian URI actions, reloading settings, and direct-launch
instructions on both architectures. Confirm no changes are made without an
explicit write action and uninstall leaves vault files intact.
