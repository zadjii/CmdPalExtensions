// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MediaControlsExtension;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Windows.Media.Control;

internal static class Program
{
    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    public static void Main()
    {
        var manager = GlobalSystemMediaTransportControlsSessionManager.Instance;
        using (var item = new MediaListItem())
        {
            var action = (TogglePlayMediaAction)item.Command!;
            Assert(item.Title == "No media playing" && action.MediaSession is null, "Empty startup must not dereference a missing session.");
            Assert(item.MoreCommands.Length == 0, "Empty sessions must not expose track actions.");

            var first = new GlobalSystemMediaTransportControlsSession("First", "Artist");
            manager.ChangeSession(first);
            Assert(item.Title == "First" && item.Subtitle == "Artist", "New playback must update track details.");
            Assert(action.MediaSession == first && item.MoreCommands.Length == 2, "Playback actions must use the current session.");
            first.ChangePlayback(GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing);
            Assert(action.Name == "Pause", "Playing sessions must offer Pause.");
            first.ChangePlayback(GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused);
            Assert(action.Name == "Play", "Paused sessions must offer Play.");
            first.ChangeTitle("Updated");
            Assert(item.Title == "Updated", "Metadata events must refresh the title.");

            var second = new GlobalSystemMediaTransportControlsSession("Second", "Other");
            manager.ChangeSession(second);
            Assert(first.Subscribers == 0 && second.Subscribers == 2, "Session switches must detach the old session.");
            Assert(action.MediaSession == second && item.Title == "Second", "Session switches must retarget commands.");
            manager.ChangeSession(null);
            Assert(second.Subscribers == 0 && action.MediaSession is null && item.MoreCommands.Length == 0, "Player exit must clear stale actions.");
            Assert(item.Title == "No media playing", "Player exit must restore the empty state.");

            manager.ChangeSession(first);
            item.Dispose();
            Assert(first.Subscribers == 0 && manager.Subscribers == 0 && action.MediaSession is null, "Disposal must release subscriptions and active actions.");
            item.Dispose();
        }

        manager.FailRequest = true;
        using (var failed = new MediaListItem())
        {
            Assert(failed.Title == "Unable to read Windows media controls", "API failure must be visible.");
            Assert(ExtensionHost.Messages.Count == 1, "API failure must be logged.");
        }

        Console.WriteLine("Media session regressions passed: empty state, playback, metadata, session switching, player exit, disposal, and API failure.");
    }
}

namespace Microsoft.CommandPalette.Extensions.Toolkit
{
    internal class CommandItem(object command)
    {
        public object? Command { get; } = command;
        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public CommandContextItem[] MoreCommands { get; set; } = [];
    }

    internal class CommandContextItem(object command)
    {
        public object Command { get; } = command;
    }

    internal class IconInfo(string value)
    {
        public string Value { get; } = value;
    }

    internal static class ExtensionHost
    {
        public static List<string> Messages { get; } = [];
        public static void LogMessage(string message) => Messages.Add(message);
    }
}

namespace MediaControlsExtension
{
    internal class TogglePlayMediaAction
    {
        public GlobalSystemMediaTransportControlsSession? MediaSession { get; set; }
        public string Name { get; set; } = "";
        public IconInfo Icon { get; set; } = new("");
    }

    internal class PrevNextTrackAction(bool previous, GlobalSystemMediaTransportControlsSession session)
    {
        public bool Previous { get; } = previous;
        public GlobalSystemMediaTransportControlsSession Session { get; } = session;
    }
}

namespace Windows.Media.Control
{
    internal class Operation<T>(Task<T> task)
    {
        public Task<T> AsTask() => task;
    }

    internal class CurrentSessionChangedEventArgs;
    internal class PlaybackInfoChangedEventArgs;
    internal class MediaPropertiesChangedEventArgs;
    internal enum GlobalSystemMediaTransportControlsSessionPlaybackStatus { Paused, Playing }
    internal record Properties(string Title, string Artist);
    internal record Playback(GlobalSystemMediaTransportControlsSessionPlaybackStatus PlaybackStatus);

    internal class GlobalSystemMediaTransportControlsSessionManager
    {
        private GlobalSystemMediaTransportControlsSession? _current;
        public static GlobalSystemMediaTransportControlsSessionManager Instance { get; } = new();
        public bool FailRequest { get; set; }
        public event Action<GlobalSystemMediaTransportControlsSessionManager, CurrentSessionChangedEventArgs>? CurrentSessionChanged;
        public int Subscribers => CurrentSessionChanged?.GetInvocationList().Length ?? 0;
        public static Operation<GlobalSystemMediaTransportControlsSessionManager> RequestAsync() => new(
            Instance.FailRequest ? Task.FromException<GlobalSystemMediaTransportControlsSessionManager>(new COMException("Test failure")) : Task.FromResult(Instance));
        public GlobalSystemMediaTransportControlsSession? GetCurrentSession() => _current;
        public void ChangeSession(GlobalSystemMediaTransportControlsSession? session)
        {
            _current = session;
            CurrentSessionChanged?.Invoke(this, new());
        }
    }

    internal class GlobalSystemMediaTransportControlsSession(string title, string artist)
    {
        private Properties _properties = new(title, artist);
        private Playback _playback = new(GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused);
        public event Action<GlobalSystemMediaTransportControlsSession, MediaPropertiesChangedEventArgs>? MediaPropertiesChanged;
        public event Action<GlobalSystemMediaTransportControlsSession, PlaybackInfoChangedEventArgs>? PlaybackInfoChanged;
        public int Subscribers => (MediaPropertiesChanged?.GetInvocationList().Length ?? 0) + (PlaybackInfoChanged?.GetInvocationList().Length ?? 0);
        public Operation<Properties> TryGetMediaPropertiesAsync() => new(Task.FromResult(_properties));
        public Playback GetPlaybackInfo() => _playback;
        public void ChangePlayback(GlobalSystemMediaTransportControlsSessionPlaybackStatus status)
        {
            _playback = new(status);
            PlaybackInfoChanged?.Invoke(this, new());
        }

        public void ChangeTitle(string title)
        {
            _properties = _properties with { Title = title };
            MediaPropertiesChanged?.Invoke(this, new());
        }
    }
}
