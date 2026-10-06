// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace TmdbExtension;

internal static class TmdbError
{
    public static IListItem CreateItem(string title, Exception exception)
    {
        var statusCode = (exception as HttpRequestException)?.StatusCode;
        ExtensionHost.LogMessage($"{title}: {exception.GetType().Name} (HTTP {statusCode}, HRESULT {exception.HResult:X8}).");

        if (statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return new ListItem(new TmdbLoginPage())
            {
                Title = "TMDB authentication failed",
                Subtitle = "Open to enter your API Read Access Token, not your TMDB password or API key.",
            };
        }

        return new ListItem(new NoOpCommand())
        {
            Title = title,
            Subtitle = exception switch
            {
                TimeoutException or OperationCanceledException => "TMDB did not respond in time. Try again.",
                JsonException or InvalidDataException => "TMDB returned an invalid response. Try again later.",
                HttpRequestException when statusCode.HasValue => $"TMDB returned HTTP {(int)statusCode.Value}. Try again later.",
                _ => "Check your connection and try again.",
            },
        };
    }
}
