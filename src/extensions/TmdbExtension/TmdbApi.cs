// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TmdbExtension;

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "It doesn't matter")]
public sealed class MovieSearchResponse
{
    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("results")]
    public MovieSearchResult[] Results { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
public sealed class MovieSearchResult
{
    [JsonPropertyName("adult")]
    public bool Adult { get; set; }

    [JsonPropertyName("backdrop_path")]
    public string Backdrop_path { get; set; }

    [JsonPropertyName("genre_ids")]
    public int[] Genre_ids { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("original_language")]
    public string Original_language { get; set; }

    [JsonPropertyName("original_title")]
    public string Original_title { get; set; }

    [JsonPropertyName("overview")]
    public string Overview { get; set; }

    [JsonPropertyName("popularity")]
    public double Popularity { get; set; }

    [JsonPropertyName("poster_path")]
    public string Poster_path { get; set; }

    [JsonPropertyName("release_date")]
    public string Release_date { get; set; }

    [JsonIgnore]
    public string ReleaseYear => Release_date.Split('-')[0];

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("video")]
    public bool Video { get; set; }

    [JsonPropertyName("vote_average")]
    public double Vote_average { get; set; }

    [JsonPropertyName("vote_count")]
    public int Vote_count { get; set; }

    [JsonPropertyName("total_pages")]
    public int Total_pages { get; set; }

    [JsonPropertyName("total_results")]
    public int Total_results { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
public sealed class MovieDetailsResponse
{
    [JsonPropertyName("adult")]
    public bool Adult { get; set; }

    [JsonPropertyName("backdrop_path")]
    public string Backdrop_path { get; set; }

    [JsonPropertyName("homepage")]
    public string Homepage { get; set; }

    [JsonPropertyName("poster_path")]
    public string Poster_path { get; set; }

    [JsonPropertyName("runtime")]
    public int Runtime { get; set; }

    [JsonPropertyName("watch/providers")]
    public AllCountryProviders Providers { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
public sealed class AllCountryProviders
{
    [JsonPropertyName("results")]
    public Dictionary<string, StreamingProviders> Countries { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
public sealed class StreamingProviders
{
    [JsonPropertyName("link")]
    public string Link { get; set; }

    [JsonPropertyName("flatrate")]
    public StreamingProvider[] Flatrate { get; set; }

    [JsonPropertyName("rent")]
    public StreamingProvider[] Rent { get; set; }

    [JsonPropertyName("buy")]
    public StreamingProvider[] Buy { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "This is sample code")]
public sealed class StreamingProvider
{
    [JsonPropertyName("logo_path")]
    public string Logo_path { get; set; }

    [JsonPropertyName("provider_id")]
    public int Provider_id { get; set; }

    [JsonPropertyName("provider_name")]
    public string Provider_name { get; set; }

    [JsonPropertyName("display_priority")]
    public int Display_priority { get; set; }
}
