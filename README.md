# Mike's Big Command Palette Extension Bundle

This repo is a collection of extensions for the Windows Command Palette. Most of
these are goof-around projects for me, just to see if it's possible. 

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

[v0.0.1](https://github.com/zadjii/CmdPalExtensions/releases/download/v0.0.1/TmdbExtension_0.0.1.0_x64.msix)
    </td>
    <td>Search for movies, and find out what streaming services they're available on.</td>
  </tr>
  <tr>
    <td>Obsidian</td>
    <td>

[v0.0.2](https://github.com/zadjii/CmdPalExtensions/releases/download/obsidian-v0.0.2/ObsidianExtension_0.0.2.0_x64.msix)
    </td>
    <td>Search your notes in Obsidian. View them in the palette & make quick edits</td>
  </tr>
  <tr>
    <td>Mastodon</td>
    <td>

[v0.0.1](https://github.com/zadjii/CmdPalExtensions/releases/download/v0.0.1/MastodonExtension_0.0.1.0_x64.msix)
    </td>
    <td>View posts on mastodon.social. You should be able to sign in and view your home timeline & favorite posts too. I haven't tested other servers yet (_I know, I'm a bad fediverse citizen_)
</td>
  </tr>
  <tr>
    <td>NFL Scores</td>
    <td>

[v0.0.1](https://github.com/zadjii/CmdPalExtensions/releases/download/v0.0.1/NflExtension_0.0.1.0_x64.msix)
    </td>
    <td>See the scores of this week's games. While games are on, should provide realtime play-by-play</td>
  </tr>
  <tr>
    <td>Edge Favorites</td>
    <td>

[v0.0.1](https://github.com/zadjii/CmdPalExtensions/releases/download/favorites-v0.0.1/EdgeFavoritesExtension_0.0.1.0_x64.msix)
    </td>
    <td>Navigate through all your bookmarks ("favorites") in Edge</td>
  </tr>
  <tr>
    <td>Segoe Icons</td>
    <td>

[v0.0.1](https://github.com/zadjii/CmdPalExtensions/releases/download/icons-v0.0.1/SegoeIconsExtension_0.0.1.0_x64.msix)
    </td>
    <td>Search the big list of Segoe Fluent icons.</td>
  </tr>
  <tr>
    <td>Media Controls</td>
    <td>

[v0.0.1](https://github.com/zadjii/CmdPalExtensions/releases/download/v0.0.1/MediaControlsExtension_0.0.1.0_x64.msix)
    </td>
    <td>Control playing media. This one's a little buggy</td>
  </tr>
  <tr>
    <td>Hacker News</td>
    <td>

[v0.0.1](https://github.com/zadjii/CmdPalExtensions/releases/download/v0.0.1/HackerNewsExtension_0.0.1.0_x64.msix)
    </td>
    <td>View top posts on Hacker News</td>
  </tr>
</tbody>
</table>

## Contributing

Want to help contribute an extension! Go for it! I'll pretty much accept PRs for anything at this point. 

To create a new extension project, you can run the following:

```pwsh
.\src\extensions\NewExtension.ps1 -name <ExtensionName> -DisplayName "<A display name>"
```

(Best practice is to have `ExtensionName` literally include the word "Extension", so that listing all extensions for cmdpal is trivial)