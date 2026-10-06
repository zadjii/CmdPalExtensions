// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Windows.Media.Control;

namespace MediaControlsExtension;

internal sealed partial class MediaListItem : CommandItem, IDisposable
{
    private readonly SemaphoreSlim _updates = new(1, 1);
    private readonly TogglePlayMediaAction _action;
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _mediaSession;
    private bool _disposed;

    public MediaListItem()
        : this(new TogglePlayMediaAction())
    {
    }

    private MediaListItem(TogglePlayMediaAction action)
        : base(action)
    {
        _action = action;
        Title = "No media playing";
        Subtitle = "Start playback in an app that supports Windows media controls.";
        _ = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        await _updates.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_disposed)
            {
                return;
            }

            if (_manager is null)
            {
                _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync().AsTask().ConfigureAwait(false);
                _manager.CurrentSessionChanged += Manager_CurrentSessionChanged;
            }

            var session = _manager.GetCurrentSession();
            if (session != _mediaSession)
            {
                DetachSession();
                _mediaSession = session;
                if (session is not null)
                {
                    session.MediaPropertiesChanged += MediaSession_MediaPropertiesChanged;
                    session.PlaybackInfoChanged += MediaSession_PlaybackInfoChanged;
                }
            }

            var action = _action;
            action.MediaSession = session;
            if (session is null)
            {
                Title = "No media playing";
                Subtitle = "Start playback in an app that supports Windows media controls.";
                action.Name = "No media playing";
                action.Icon = new("\ue768");
                MoreCommands = [];
                return;
            }

            var properties = await session.TryGetMediaPropertiesAsync().AsTask().ConfigureAwait(false);
            Title = string.IsNullOrEmpty(properties?.Title) ? "Media controls" : properties.Title;
            Subtitle = properties?.Artist ?? string.Empty;
            var playing = session.GetPlaybackInfo().PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
            action.Name = playing ? "Pause" : "Play";
            action.Icon = new(playing ? "\ue769" : "\ue768");
            MoreCommands = [
                new CommandContextItem(new PrevNextTrackAction(true, session)),
                new CommandContextItem(new PrevNextTrackAction(false, session))
            ];
        }
        catch (Exception ex) when (ex is COMException or UnauthorizedAccessException or InvalidOperationException)
        {
            ExtensionHost.LogMessage($"Unable to read Windows media controls: {ex.Message}");
            Title = "Unable to read Windows media controls";
            Subtitle = "Check that a supported media app is running, then restart Command Palette.";
            _action.MediaSession = null;
            MoreCommands = [];
        }
        finally
        {
            _updates.Release();
        }
    }

    private void Manager_CurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args) => _ = RefreshAsync();

    private void MediaSession_PlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args) => _ = RefreshAsync();

    private void MediaSession_MediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args) => _ = RefreshAsync();

    private void DetachSession()
    {
        if (_mediaSession is not null)
        {
            _mediaSession.MediaPropertiesChanged -= MediaSession_MediaPropertiesChanged;
            _mediaSession.PlaybackInfoChanged -= MediaSession_PlaybackInfoChanged;
            _mediaSession = null;
        }
    }

    public void Dispose()
    {
        _updates.Wait();
        try
        {
            _disposed = true;
            if (_manager is not null)
            {
                _manager.CurrentSessionChanged -= Manager_CurrentSessionChanged;
            }

            DetachSession();
            _action.MediaSession = null;
        }
        finally
        {
            _updates.Release();
        }
    }
}
