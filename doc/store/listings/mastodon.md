# Mastodon for Command Palette

**Draft; reservation and Store ID pending.** Proposed package:
`zadjii.MastodonforCommandPalette`. Use the [shared fields](../README.md).

## Description

Browse your chosen Mastodon instance from PowerToys Command Palette. View public
posts, or authorize your account to access your home timeline and interact with
posts. Set Home instance in the extension settings; it defaults to mastodon.social.
Requires PowerToys Command Palette and internet access. This is an independent
community client, not an official Mastodon app.

## Features and keywords

Public and home timelines; replies; favorites; boosts.
Keywords: Mastodon, mastodon.social, social, PowerToys.

## Submission and reviewer notes

Proposed category: Social. Logo: `../assets/mastodon-store-logo.png`.
Screenshots: public timeline and authorized test account with permission.
Privacy: [Mastodon](../../../PRIVACY.md#mastodon). OAuth `read write push` scopes;
credentials are in Windows Credential Locker. `runFullTrust` supports desktop
COM activation and credential access. Disclose internet/user-generated content
in ratings and review Mastodon's client/branding terms.

Test instance selection and validation, separate saved logins when switching
instances, public access, first authorization, denied authorization, home timeline,
reply/favorite/boost with a consenting test account, expired/revoked tokens,
selected-instance logout, offline behavior, and direct-launch help on both architectures. Supply
reviewer account access securely if certification requests it, never in this
repository or public screenshots.
