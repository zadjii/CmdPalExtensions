// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace SpongeBot;

public partial class SpongeBotCommandsProvider : CommandProvider
{
    private readonly FallbackSpongeTextItem _fallbackSpongeTextItem = new();

    public SpongeBotCommandsProvider()
    {
        DisplayName = "Spongebob, mocking";
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.scale-400.png");
        Frozen = false;
    }

    // private readonly SpongebotPage mainPage = new();

    // private readonly SpongebotSettingsPage settingsPage = new();
    public override ICommandItem[] TopLevelCommands() =>

        // var settingsPath = SpongebotPage.StateJsonPath();
        // return !File.Exists(settingsPath)
        //    ? [new CommandItem(settingsPage) { Title = "Spongebot settings", Subtitle = "Enter your imgflip credentials" }]
        //    : [];
        [];

    public override IFallbackCommandItem[]? FallbackCommands() =>

        // var settingsPath = SpongebotPage.StateJsonPath();
        // if (!File.Exists(settingsPath))
        // {
        //    return null;
        // }

        // var listItem = new FallbackCommandItem(mainPage, displayTitle: "Spongebob, mocking")
        // {
        //    MoreCommands = [
        //        new CommandContextItem(mainPage.CopyCommand),
        //        new CommandContextItem(settingsPage),
        //    ],
        // };
        // return [listItem];
        [_fallbackSpongeTextItem];
}

internal sealed partial class FallbackSpongeTextItem : FallbackCommandItem
{
    private readonly CopyTextCommand _copyCommand = new(string.Empty);

    public override ICommand? Command => _copyCommand;

    public FallbackSpongeTextItem()
        : base(new NoOpCommand(), "Convert text to mOcKiNg CaSe")
    {
        Title = string.Empty;
        Icon = new IconInfo("https://imgflip.com/s/meme/Mocking-Spongebob.jpg");
    }

    public override void UpdateQuery(string query)
    {
        Title = _copyCommand.Text = ConvertToAlternatingCase(query);
        _copyCommand.Name = string.IsNullOrWhiteSpace(query) ? string.Empty : "Copy";
    }

    internal static string ConvertToAlternatingCase(string input)
    {
        StringBuilder sb = new();
        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];
#pragma warning disable CA1304 // Specify CultureInfo
            sb.Append(i % 2 == 0 ? char.ToUpper(c) : char.ToLower(c));
#pragma warning restore CA1304 // Specify CultureInfo
        }

        return sb.ToString();
    }
}
