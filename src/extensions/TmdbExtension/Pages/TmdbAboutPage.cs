// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace TmdbExtension;

internal sealed partial class TmdbAboutPage : ContentPage
{
    public TmdbAboutPage()
    {
        Name = "About and data attribution";
        Title = "About TMDB Search";
        Icon = IconHelpers.FromRelativePath("Assets\\Tmdb-312x276-logo.png");
    }

    public override IContent[] GetContent() => [
        new MarkdownContent
        {
            Body = """
                # TMDB Search for Command Palette

                This product uses the TMDB API but is not endorsed or certified by TMDB.

                Movie details and artwork: [The Movie Database (TMDB)](https://www.themoviedb.org/).
                Watch-provider availability: powered by [JustWatch](https://www.justwatch.com/).
                Follow the watch-provider link in movie details for current availability.

                This independent extension requires your own TMDB API Read Access Token.

                [Privacy policy](https://github.com/zadjii/CmdPalExtensions/blob/main/PRIVACY.md#tmdb-search)
                | [Third-party notices](https://github.com/zadjii/CmdPalExtensions/blob/main/THIRD-PARTY-NOTICES.md)
                | [Support](https://github.com/zadjii/CmdPalExtensions/issues)
                """,
        }
    ];
}
