// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Timers;
using HtmlAgilityPack;
using Microsoft.CmdPal.Extensions;
using Microsoft.CmdPal.Extensions.Helpers;
using PuppeteerSharp;
using Svg;

namespace NflExtension;

internal sealed partial class NflExtensionPage : ListPage, IDisposable
{
    private static readonly BrowserFetcher _browserFetcher = new();
    private static IBrowser _browser;

    internal static readonly HttpClient Client = new();
    internal static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };
    private Timer _timer;
    private List<ListItem> _lastItems;

    static NflExtensionPage()
    {
        // Download Chromium browser for Puppeteer Sharp
        _ = Task.Run(async () =>
        {
            await _browserFetcher.DownloadAsync().ConfigureAwait(false);
            _browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
        });
    }

    public NflExtensionPage()
    {
        Icon = new("🏈");
        Name = "NFL Scores";
        ShowDetails = true;
        IsLoading = true;
    }

    public override IListItem[] GetItems()
    {
        if (_lastItems == null)
        {
            var dataAsync = FetchWeekAsync();
            dataAsync.ConfigureAwait(false);
            _lastItems = dataAsync.Result;
        }

        if (_timer == null)
        {
            // Set up the timer to trigger every 30 seconds (30,000 milliseconds)
            _timer = new(10000);
            _timer.Elapsed += OnTimedEvent;
            _timer.AutoReset = true; // Keep repeating
            _timer.Enabled = true;
        }

        IsLoading = false;
        return [.. _lastItems];
    }

    private void OnTimedEvent(object source, ElapsedEventArgs e)
    {
        var dataAsync = FetchWeekAsync();
        dataAsync.ConfigureAwait(false);
        _lastItems = ReplaceGames(_lastItems, dataAsync.Result);

        // _lastItems = dataAsync.Result;
        this.RaiseItemsChanged(_lastItems.Count);
    }

    private static List<ListItem> ReplaceGames(List<ListItem> oldItems, List<ListItem> newItems)
    {
        var list = new List<ListItem>();

        foreach (var n in newItems)
        {
            var found = false;
            foreach (var o in oldItems)
            {
                if (o != null
                    && n != null
                    && o.Command is NflGameCommand oldCmd
                    && n.Command is NflGameCommand newCmd
                    && oldCmd.Game.Id == newCmd.Game.Id)
                {
                    found = true;
                    o.Command = n.Command;
                    o.Subtitle = n.Subtitle;
                    o.Title = n.Title;
                    o.Tags = n.Tags;
                    o.MoreCommands = n.MoreCommands;
                    o.Details = n.Details;
                    list.Add(o);
                }
            }

            if (!found)
            {
                list.Add(n);
            }
        }

        return list;
    }

    private ListItem EventNodeToItem(NflEvent e)
    {
        var name = e.Name;
        var game = e
                .Competitions
                    .First<Competition>();
        var id = game.Id;

        Tag[] tags = [];
        if (game.Situation != null || e.Status.Type.Name == "STATUS_FINAL")
        {
            tags = game
                        .Competitors
                        .Select(c =>
                            new Tag()
                            {
                                Icon = new IconDataType(c.Team.Id == game.Situation?.Possession ? "🏈" : string.Empty),
                                Text = $"{c.Team.Abbreviation} {c.Score}",
                                Foreground = HexToColor(c.Team.AlternateColor),
                                Background = HexToColor(c.Team.Color),
                            }).ToArray();
        }

        var details = BuildDetails(game);

        // Icon
        var icon = new IconDataType(string.Empty);
        if (game.Situation != null)
        {
            icon = new(game.Situation.IsRedZone ? "🚨" : "🟢");
        }
        else if (e.Status.Type.Name == "STATUS_FINAL")
        {
            var winner = game.Competitors[0].Winner ? game.Competitors[0] : game.Competitors[1];
            icon = new(winner.Team.Logo);
        }
        else
        {
            icon = new("\uecc5");
        }

        return new ListItem(new NflGameCommand(game))
        {
            Title = name,
            Subtitle = e.Status.Type.Detail,
            Icon = icon,
            Tags = tags,
            Details = details,
        };

        static IDetails BuildDetails(Competition game)
        {
            IDetails details = null;
            if (game.Situation != null)
            {
                // var driveChartPath = await FetchImage(game);

                // var uri = new Uri(driveChartPath).AbsoluteUri;
                var detailsBody = $"""
{game.Situation.DownDistanceText}

{game.Situation.LastPlay.Text}
""";

                details = new GameDetails(game)
                {
                    Title = string.Join("-", game.Competitors.Select(c => $"{c.Team.Abbreviation} {c.Score}")),
                    Body = detailsBody,

                    // HeroImage = new(driveChartPath),
                };
            }

            return details;
        }
    }

    public static async Task<string> FetchImage(Competition game)
    {
        var url = $"https://www.espn.com/nfl/game/_/gameId/{game.Id}";
        var rand = new Random().Next(int.MaxValue);
        var svgOutputPath = $"Assets/games/{game.Id}_{rand}.svg";
        var pngOutputPath = $"Assets/games/{game.Id}_{rand}.png";
        var externalCssUrl = "https://cdn1.espn.net/fitt/5c11ba92527d-release-12-11-2024.2.0.1772/client/espnfitt/css/3246-55ffc5e5.css";

        _ = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory.ToString(), svgOutputPath);
        pngOutputPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory.ToString(), pngOutputPath);
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory.ToString(), "Assets/games"));
        try
        {
            // Create an HttpClient to retrieve the webpage
            using var httpClient = new HttpClient();
            var response = await httpClient.GetStringAsync(url);

            // Download the external CSS file
            var cssResponse = await httpClient.GetStringAsync(externalCssUrl);

            // Load the response into an HtmlDocument
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(response);

            // Select the #drivechart element
            var driveChartElement = htmlDocument.DocumentNode.SelectSingleNode("//*[@id='drivechart']");

            if (driveChartElement != null)
            {
                var svg = await FetchSvg(game);
                if (!string.IsNullOrEmpty(svg))
                {
                    svg = svg.Replace("xlink:href", "href");
                    var svgDocument = SvgDocument.FromSvg<SvgDocument>(svg);
                    var bitmap = svgDocument.Draw();
                    bitmap.Save(pngOutputPath, ImageFormat.Png);
                    return System.IO.Path.GetFullPath(pngOutputPath);
                }

                return string.Empty;

                // // Extract the inner HTML content
                //                var inner = driveChartElement.InnerHtml;

                // // svgContent = $"<?xml version=\"1.0\" encoding=\"UTF-8\"?><svg xmlns=\"http://www.w3.org/2000/svg\" fill=\"none\" height=\"100%\" width=\"100%\" viewBox=\"0 0 512 512\">{svgContent}</svg>";

                // // Wrap the SVG content and include the downloaded CSS styles
                //                inner = inner.Replace("xlink:href", "href");
                //                var svgWithStyles = $@"
                // <svg xmlns=""http://www.w3.org/2000/svg"" xmlns:xlink=""http://www.w3.org/1999/xlink"">
                //    <style>
                //        {cssResponse}
                //    </style>
                //    {inner}
                // </svg>";

                // var outer = driveChartElement.OuterHtml;
                //                var viewboxString = driveChartElement.Attributes["viewbox"].Value ?? "0 0 512 512";
                //                var viewboxElements = viewboxString.Split(" ");

                // outer = outer.Replace("xlink:href", "href");
                //                var svgDocument = SvgDocument.FromSvg<SvgDocument>(svgWithStyles);

                // svgDocument.ViewBox = new SvgViewBox(
                //                     float.Parse(viewboxElements[0], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture),
                //                     float.Parse(viewboxElements[1], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture),
                //                     float.Parse(viewboxElements[2], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture),
                //                     float.Parse(viewboxElements[3], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture));

                // var bitmap = svgDocument.Draw();
                //                bitmap.Save(pngOutputPath, ImageFormat.Png);

                // //// Save the content as an SVG file
                //                System.IO.File.WriteAllText(svgOutputPath, svgWithStyles);

                // // Debug.WriteLine($"SVG saved successfully to {svgOutputPath}");
                //                // return Path.GetFullPath(svgOutputPath);
                // return System.IO.Path.GetFullPath(pngOutputPath);
            }
            else
            {
                Debug.WriteLine("The #drivechart element was not found on the page.");
            }
        }
        catch (System.Exception)
        {
            // Console.WriteLine($"An error occurred: {ex.Message}");
        }

        return string.Empty;
    }

    private static async Task<string> FetchSvg(Competition game)
    {
        var url = $"https://www.espn.com/nfl/game/_/gameId/{game.Id}";
        if (_browser == null)
        {
            return string.Empty;
        }

        // Launch a headless browser
        using var page = await _browser.NewPageAsync();

        // Navigate to the URL
        await page.GoToAsync(url, WaitUntilNavigation.Networkidle2);

        // Wait for the #drivechart element to load
        await page.WaitForSelectorAsync("#drivechart");

        // Extract the #drivechart element with computed styles inlined
        var svgContent = await page.EvaluateFunctionAsync<string>(@"() => {
            const element = document.querySelector('#drivechart');
            if (!element) return null;

            // Clone the element to include styles
            const clonedElement = element.cloneNode(true);

            // Get computed styles and inline them
            const allElements = clonedElement.querySelectorAll('*');
            for (const el of allElements) {
                const computed = getComputedStyle(el);
                for (const key of computed) {
                    el.style[key] = computed[key];
                }
            }

            // Return the outer HTML with inlined styles
            return clonedElement.outerHTML;
        }");

        //// Save the styled SVG to a file
        // if (!string.IsNullOrEmpty(svgContent))
        // {
        //    await File.WriteAllTextAsync(outputPath, svgContent);
        //    Console.WriteLine($"Final styled SVG saved to {outputPath}");
        // }
        // else
        // {
        //    Console.WriteLine("The #drivechart element was not found or could not be extracted.");
        // }

        //// Close the browser
        // await browser.CloseAsync();
        return svgContent;
    }

    // private static async Task<string> FetchSvg(Competition game)
    // {
    //    var url = $"https://www.espn.com/nfl/game/_/gameId/{game.Id}";

    // // Launch a headless browser using Playwright
    //    using var playwright = await Playwright.CreateAsync();
    //    await using var browser = await playwright.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    //    var context = await browser.NewContextAsync();
    //    var page = await context.NewPageAsync();

    // // Navigate to the page
    //    await page.GotoAsync(url);

    // // Wait for the #drivechart element to load (optional delay in case of JavaScript rendering)
    //    await page.WaitForSelectorAsync("#drivechart");

    // // Extract the outer HTML of the #drivechart SVG element with all computed styles applied
    //    var svgContent = await page.EvaluateAsync<string>(@"
    //        () => {
    //            const element = document.querySelector('#drivechart');
    //            if (!element) return null;

    // // Clone the element to include styles
    //            const clonedElement = element.cloneNode(true);

    // // Get computed styles and inline them
    //            const allElements = clonedElement.querySelectorAll('*');
    //            const computedStyle = getComputedStyle(clonedElement);
    //            for (const el of allElements) {
    //                const computed = getComputedStyle(el);
    //                for (const key of computed) {
    //                    el.style[key] = computed[key];
    //                }
    //            }
    //            // Return the outer HTML with inlined styles
    //            return clonedElement.outerHTML;
    //        }
    //    ");

    // if (!string.IsNullOrEmpty(svgContent))
    //    {
    //        return svgContent;
    //    }
    //    else
    //    {
    //        Console.WriteLine("The #drivechart element was not found or could not be extracted.");
    //    }

    // return string.Empty;
    // }
    private static OptionalColor HexToColor(string hex)
    {
        // Ensure the string has the correct length
        if (hex.Length is not 6 and not 8)
        {
            throw new ArgumentException("Hex string must be 6 or 8 characters long.");
        }

        // If the string is 6 characters, assume it's RGB and prepend alpha as FF
        if (hex.Length == 6)
        {
            hex = "FF" + hex;
        }

        // Parse the hex values into bytes
        var a = Convert.ToByte(hex[..2], 16); // Alpha
        var r = Convert.ToByte(hex.Substring(2, 2), 16); // Red
        var g = Convert.ToByte(hex.Substring(4, 2), 16); // Green
        var b = Convert.ToByte(hex.Substring(6, 2), 16); // Blue

        return ColorHelpers.FromArgb(a, r, g, b);
    }

    private static List<string> DaysOfWeek()
    {
        var days = new List<string>();

        // Get today's date
        var today = DateTime.Today;

        // Find the Tuesday before today
        var daysToTuesday = ((int)today.DayOfWeek - (int)DayOfWeek.Tuesday + 7) % 7;
        var startDate = today.AddDays(-daysToTuesday);

        // Iterate through the days of the week starting from the Tuesday before today
        for (var i = 0; i < 7; i++)
        {
            var currentDay = startDate.AddDays(i);
            var formattedDate = currentDay.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            days.Add(formattedDate);
        }

        return days;
    }

    private async Task<List<ListItem>> FetchWeekAsync()
    {
        var days = DaysOfWeek();
        var games = new List<ListItem>();
        foreach (var day in days)
        {
            var gameData = await FetchDataAsync(day);
            foreach (var ev in gameData.Events)
            {
                var li = EventNodeToItem(ev);
                games.Add(li);
            }

            // games.AddRange((IEnumerable<ListItem>)gameData.Events.Select(EventNodeToItemAsync));
        }

        return games;
    }

    private async Task<NflData> FetchDataAsync(string date)
    {
        _ = DaysOfWeek();

        try
        {
            // Make a GET request to the Mastodon trends API endpoint
            var response = await Client
                .GetAsync($"https://site.api.espn.com/apis/site/v2/sports/football/nfl/scoreboard?dates={date}");
            response.EnsureSuccessStatusCode();

            // Read and deserialize the response JSON into a list of MastodonStatus objects
            var responseBody = await response.Content.ReadAsStringAsync();

            var data = JsonSerializer.Deserialize<NflData>(responseBody, Options);

            return data;
        }
        catch (System.Exception e)
        {
            Console.WriteLine($"An error occurred: {e.Message}");
        }

        return null;
    }

    public void Dispose() => throw new NotImplementedException();
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
public partial class MuhDetails : BaseObservable, IDetails
{
    private IconDataType _heroImage = new(string.Empty);
    private string _title = string.Empty;
    private string _body = string.Empty;
    private IDetailsElement[] _metadata = [];

    public virtual IconDataType HeroImage
    {
        get => _heroImage;
        set
        {
            _heroImage = value;
            OnPropertyChanged(nameof(HeroImage));
        }
    }

    public string Title
    {
        get => _title;
        set
        {
            _title = value;
            OnPropertyChanged(nameof(Title));
        }
    }

    public string Body
    {
        get => _body;
        set
        {
            _body = value;
            OnPropertyChanged(nameof(Body));
        }
    }

    public IDetailsElement[] Metadata
    {
        get => _metadata;
        set
        {
            _metadata = value;
            OnPropertyChanged(nameof(Metadata));
        }
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
public partial class GameDetails : MuhDetails
{
    private readonly Competition _game;
    private bool fetchedIcon;

    public GameDetails(Competition game)
    {
        _game = game;
    }

    public override IconDataType HeroImage
    {
        get
        {
            if (fetchedIcon)
            {
                return base.HeroImage;
            }
            else
            {
                _ = Task.Run(() =>
                {
                    var t = NflExtensionPage.FetchImage(_game);
                    t.ConfigureAwait(false);
                    base.HeroImage = new(t.Result);
                });
                fetchedIcon = true;
            }

            return base.HeroImage;
        }

        set => base.HeroImage = value;
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
public sealed partial class NflGameCommand : InvokableCommand
{
    private readonly OpenUrlCommand _command;

    public Competition Game { get; init; }

    public string GameUrl => $"https://www.espn.com/nfl/game/_/gameId/{Game.Id}";

    public NflGameCommand(Competition game)
    {
        _command = new OpenUrlCommand($"https://www.espn.com/nfl/game/_/gameId/{game.Id}") { Name = "View on ESPN" };
        Game = game;
    }

    public override ICommandResult Invoke() => _command.Invoke();
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
public sealed class NflData
{
    [JsonPropertyName("events")]
    public List<NflEvent> Events { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
public sealed class NflEvent
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("status")]
    public NflEventStatus Status { get; set; }

    [JsonPropertyName("competitions")]
    public List<Competition> Competitions { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
public sealed class Competition
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("competitors")]
    public List<Competitor> Competitors { get; set; }

    [JsonPropertyName("situation")]
    public CompetitionSituation Situation { get; set; } = null;

    [JsonPropertyName("broadcast")]
    public string Broadcast { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
public sealed class Competitor
{
    [JsonPropertyName("team")]
    public Team Team { get; set; }

    [JsonPropertyName("score")]
    public string Score { get; set; }

    [JsonPropertyName("winner")]
    public bool Winner { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
public sealed class Team
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("abbreviation")]
    public string Abbreviation { get; set; }

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; }

    [JsonPropertyName("shortDisplayName")]
    public string ShortDisplayName { get; set; }

    [JsonPropertyName("color")]
    public string Color { get; set; }

    [JsonPropertyName("alternateColor")]
    public string AlternateColor { get; set; }

    [JsonPropertyName("logo")]
    public string Logo { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
public sealed class CompetitionSituation
{
    [JsonPropertyName("downDistanceText")]
    public string DownDistanceText { get; set; }

    [JsonPropertyName("shortDownDistanceText")]
    public string ShortDownDistanceText { get; set; }

    [JsonPropertyName("possessionText")]
    public string PossessionText { get; set; }

    [JsonPropertyName("isRedZone")]
    public bool IsRedZone { get; set; }

    [JsonPropertyName("homeTimeouts")]
    public int HomeTimeouts { get; set; }

    [JsonPropertyName("awayTimeouts")]
    public int AwayTimeouts { get; set; }

    [JsonPropertyName("possession")]
    public string Possession { get; set; }

    [JsonPropertyName("lastPlay")]
    public LastPlay LastPlay { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
public sealed class LastPlay
{
    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("drive")]
    public Drive Drive { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
public sealed class Drive
{
    [JsonPropertyName("description")]
    public string Description { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
public sealed class NflEventStatus
{
    [JsonPropertyName("type")]
    public NflEventStatusType Type { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
public sealed class NflEventStatusType
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("detail")]
    public string Detail { get; set; }

    [JsonPropertyName("shortDetail")]
    public string ShortDetail { get; set; }
}
