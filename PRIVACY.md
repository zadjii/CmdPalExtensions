# Privacy policy for Command Palette extensions

This policy covers the nine extensions in this repository, distributed as
separate apps by Michael Griese (`zadjii.`). It applies to the Store-preparation
versions described below. Microsoft PowerToys, Windows, Microsoft Store,
browsers, and external services have their own privacy policies.

## Common behavior

These extensions do not implement advertising, analytics, or a developer-operated
data collection service. They process commands through PowerToys Command Palette.
Some features contact the third-party services below. Those requests expose your
IP address and normal connection/request information to the service and its
infrastructure. Remote images may be fetched by Command Palette on the
extension's behalf.

Errors and diagnostic messages can be written to Command Palette's local logs.
Depending on the feature, these may include service error text, URLs, or local
file paths. Review logs before sharing them publicly. Nothing here promises that
the host, operating system, browser, or third-party services do not collect data.

## Hacker News

Opening the Hacker News page requests top stories and story metadata from
`hacker-news.firebaseio.com`. Its icon is loaded from `news.ycombinator.com`.
The extension also checks article sites for
`/favicon.ico`, and Command Palette can download those icons. **Article hosts may
receive these requests before you select or open a story.** Opening a story or
comments sends you to the corresponding website in your default browser.
No Hacker News account, saved reading history, or developer server is used.

## Obsidian Notes

You choose a local vault folder. Its path is stored in the extension's local
JSON settings. The extension searches Markdown files, reads note contents for
previews, and writes or appends to notes when you explicitly invoke those actions.
It ignores `.git` and `.obsidian` directories during note discovery. Opening a
note uses Obsidian's URI handler. The extension does not upload your vault to a
developer server; however, linked remote images in rendered Markdown may cause
the host to make network requests. Obsidian and any synchronization you configure
operate independently.

## Mastodon

You choose your home instance in the extension's local JSON settings; the
default is **mastodon.social**. The selected instance receives requests for
timelines, posts, profiles, and account actions. Avatars and attached media may
be hosted by other instances or content hosts. Signing in registers an OAuth
application with your selected instance and authorizes account access with
`read write push` scopes. The application credentials and your access token are
stored separately per instance in Windows Credential Locker (`PasswordVault`;
the default instance retains the legacy `MastodonExtensionKeys` resource).
Replies, favorites, and boosts are sent to the service when you request them.
Changing instances switches to that instance's saved login if available.
Signing out removes only the selected instance's user token; application
registration credentials and other instances' logins may remain. You can also
revoke authorization in each Mastodon account's settings. Your chosen instance's
privacy policy applies; see [mastodon.social's policy](https://mastodon.social/privacy-policy)
when using the default instance.

## TMDB Search

You supply a TMDB API Read Access Token. It is stored in Windows Credential
Locker (`TmdbExtensionKeys`, `BearerToken`) and sent to `api.themoviedb.org` to
authorize requests. Search queries and requested movie identifiers are sent to
TMDB. Your configured result language is stored in local JSON settings and sent
with movie searches and detail requests. Posters and provider logos can be downloaded from `image.tmdb.org`, and
TMDB/JustWatch links or icons can contact their hosts. Links open in your browser.
You can revoke the token in your TMDB account; clearing the credential separately
may be necessary. See [TMDB's privacy policy](https://www.themoviedb.org/privacy-policy)
and [JustWatch's privacy policy](https://www.justwatch.com/us/privacy-policy).

## Edge Favorites

This extension reads the local `Default` profile Bookmarks JSON for supported
Microsoft Edge Stable, Beta, Dev, and Canary installations. Bookmark titles,
folders, and URLs are searched locally; it does not read browser passwords or
browsing history. The current browser-channel icons are loaded from Wikimedia
Commons. Selecting a favorite opens its URL in your browser. The extension does
not upload your bookmark collection to a developer server.

## NFL Scores

The extension requests ESPN's public scoreboard API, including the requested
date, and displays game information and remote team logos. The scoreboard
refreshes approximately every ten seconds while its page is active. Game links
open ESPN in your browser. No sports-service account or location permission is
required. See [Disney's privacy policy](https://privacy.thewaltdisneycompany.com/)
for ESPN services.

## Segoe Icons

Icon names and code points are searched locally from the bundled icon list.
Choosing a copy action writes the selected character or XAML text to the Windows
clipboard, which may be synchronized if you enabled Windows clipboard history
or cloud synchronization. Documentation links open Microsoft Learn or the
installed WinUI Gallery on request. The extension does not upload searches.

## SpongeBot

Input text is converted to alternating case locally. A copy action writes the
result to the Windows clipboard; Windows clipboard history/synchronization
settings apply. This extension does not send the input to a network service and
does not fetch the former remotely hosted meme image.

## Media Controls

Windows media-control APIs expose the active playback session, track title,
artist, and playback status. This extension displays that information and sends
play/pause/previous/next requests to the selected local session. It does not
upload media information to a developer server. The media player and its
streaming service have their own policies.

## Retention, deletion, and identity changes

In-memory results normally disappear when the extension process exits. Local
settings and Windows Credential Locker entries persist until removed; uninstall
is not a guarantee that all credentials, host caches, clipboard data, logs, or
third-party records are deleted. Vault notes and browser bookmarks are your
existing files and are not removed by uninstalling an extension.

The new Store package identities are separate from the old sideloaded packages.
Automatic settings or credential migration is not implemented. Back up local
settings before removing the old package, reselect your vault, and sign in or
configure API tokens again as needed. Service-side data remains subject to the
service's retention and deletion controls.

## Contact and changes

For questions or deletion assistance relating to these extensions, use
[the repository's support/issues page](https://github.com/zadjii/CmdPalExtensions/issues).
Do not post tokens, private notes, or other sensitive data in a public issue.
Changes to this policy are recorded in the repository's history.
