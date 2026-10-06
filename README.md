# Mike's Big Command Palette Extension Bundle

This repo is a collection of extensions for the Windows Command Palette. Most of
these are goof-around projects for me, just to see if it is possible. 

These are open for community contributions. I definitely don't have the time to flush out the features of all these myself, but I'm happy to accept PRs.

## Extensions

<table><thead>
  <tr>
    <th>Extension</th>
    <th>x64 Link</th>
    <th>Description</th>
  </tr></thead>
<tbody>
  <tr>
    <td>TMDB Search</td>
    <td>

[v0.0.4](https://github.com/zadjii/CmdPalExtensions/releases/download/tmdb%2Fv0.0.4/TmdbExtension_0.0.4.0_x64.msix)
    </td>
    <td>Search for movies, and find out what streaming services they're available on.</td>
  </tr>
  <tr>
    <td>Obsidian</td>
    <td>

[v0.0.5](https://github.com/zadjii/CmdPalExtensions/releases/download/obsidian%2Fv0.0.5/ObsidianExtension_0.0.5.0_x64.msix)
    </td>
    <td>Search your notes in Obsidian. View them in the palette & make quick edits</td>
  </tr>
  <tr>
    <td>Mastodon</td>
    <td>

[v0.0.4](https://github.com/zadjii/CmdPalExtensions/releases/download/mastodon%2Fv0.0.4/MastodonExtension_0.0.4.0_x64.msix)
    </td>
    <td>Explore posts on your Mastodon instance, sign in to view your home timeline, and favorite or boost posts. Choose your home instance in the extension settings (defaults to mastodon.social).
</td>
  </tr>
  <tr>
    <td>Segoe Icons</td>
    <td>

[v0.0.3](https://github.com/zadjii/CmdPalExtensions/releases/download/icons%2Fv0.0.3/SegoeIconsExtension_0.0.3.0_x64.msix)
    </td>
    <td>Search the big list of Segoe Fluent icons.</td>
  </tr>
  <tr>
    <td>Hacker News</td>
    <td>

[v0.0.5](https://github.com/zadjii/CmdPalExtensions/releases/download/hackernews%2Fv0.0.5/HackerNewsExtension_0.0.5.0_x64.msix)
    </td>
    <td>View top posts on Hacker News</td>
  </tr>
  <tr>
    <td>Media Controls (BROKEN)</td>
    <td>

[v0.0.1](https://github.com/zadjii/CmdPalExtensions/releases/download/v0.0.1/MediaControlsExtension_0.0.1.0_x64.msix)
    </td>
    <td>Control playing media. This one is buggy, and hasn't been updated since early CmdPal builds. It needs love</td>
  </tr>
</tbody>
</table>

### Mastodon home instance

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
`dotnet test .\src\extensions\MastodonExtension.Tests\MastodonExtension.Tests.csproj -p:Platform=x64`.

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

Want to help contribute an extension! Go for it! I'll pretty much accept PRs for anything at this point. 

Run the TMDB regression tests on Windows with the .NET 9 SDK or later:

```powershell
dotnet test .\src\extensions\TmdbExtension.Tests\TmdbExtension.Tests.csproj -p:Platform=x64
```

These tests use simulated HTTP responses; no TMDB account or API token is required.
