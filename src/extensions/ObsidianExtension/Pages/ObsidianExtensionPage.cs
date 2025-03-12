// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Windows.Foundation;
using Windows.System;

namespace ObsidianExtension;

internal sealed partial class ObsidianExtensionPage : ListPage
{
    public static readonly IconInfo ObsidianIcon = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory.ToString(), "Assets\\obsidian-logo.png"));

    public ObsidianExtensionPage()
    {
        // Icon = new(@"https://obsidian.md/images/obsidian-logo-gradient.svg");
        Icon = ObsidianIcon;
        Name = "Obsidian Notes";
        PlaceholderText = "Search notes...";
        IsLoading = true;
        ShowDetails = true;

        SettingsManager.Instance.Settings.SettingsChanged += (s, e) => RaiseItemsChanged(0);
    }

    public override IListItem[] GetItems()
    {
        var vaultPath = SettingsManager.Instance.VaultPath;
        if (string.IsNullOrEmpty(vaultPath)
            || !Directory.Exists(vaultPath))
        {
            var item = new ListItem(SettingsManager.Instance.Settings.SettingsPage)
            {
                Title = "Open settings",
                Subtitle = "You need to set the path to your vault first",
            };
            IsLoading = false;
            return [item];
        }

        var notes = GetNotes(vaultPath);
        IsLoading = false;

        // Vault name (use the last folder in the vault path)
        var vaultName = Path.GetFileName(vaultPath);

        var listItems = notes.Select(n =>
        {
            var openNote = new OpenUrlCommand(n.ObsidianProtocolUri)
            {
                Name = "Open",
                Result = CommandResult.Dismiss(),
            };

            var previewNote = new PreviewNotePage(n);
            var details = n.Details;

            // details.Title = n.Name;
            return new ListItem(previewNote)
            {
                Title = n.Name,
                Subtitle = $"{n.Folder}/",
                Icon = previewNote.Icon,
                MoreCommands = [
                    new CommandContextItem(openNote) { RequestedShortcut = new(VirtualKeyModifiers.Control, (int)VirtualKey.O, 0) },
                    new CommandContextItem(new EditNotePage(n)) { RequestedShortcut = new(VirtualKeyModifiers.Control, (int)VirtualKey.E, 0) },
                    new CommandContextItem(new AppendToNotePage(n)) { RequestedShortcut = new(VirtualKeyModifiers.Control, (int)VirtualKey.A, 0) },
                ],
                Details = details,
            };
        });
        return listItems.ToArray();
    }

    private IEnumerable<Note> GetNotes(string vaultPath)
    {
        var notes = new List<Note>();

        if (Directory.Exists(vaultPath))
        {
            // Get all markdown files in the directory and subdirectories
            var noteFiles = Directory.GetFiles(vaultPath, "*.md", SearchOption.AllDirectories);

            foreach (var noteFile in noteFiles)
            {
                // Display the note name without the full path or file extension
                var n = new Note(Path.GetFullPath(noteFile))
                {
                    VaultPath = vaultPath,
                };
                n.ContentChanged += (s, e) => this.RaiseItemsChanged(0);
                notes.Add(n);
            }
        }

        return notes.OrderByDescending(n => n.LastModified);
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Sample code")]
public partial class PreviewNotePage : ContentPage
{
    private readonly Note _note;

    public PreviewNotePage(Note note)
    {
        _note = note;
        Name = "Preview";
        Title = _note.Name;
        Icon = new("\uE70B");

        var openNote = new OpenUrlCommand(note.ObsidianProtocolUri)
        {
            Name = "Open",
            Result = CommandResult.Dismiss(),
        };
        Commands = [
            new CommandContextItem(openNote) { RequestedShortcut = new(VirtualKeyModifiers.Control, (int)VirtualKey.O, 0) },
            new CommandContextItem(new EditNotePage(note)) { RequestedShortcut = new(VirtualKeyModifiers.Control, (int)VirtualKey.E, 0) },
            new CommandContextItem(new AppendToNotePage(note)) { RequestedShortcut = new(VirtualKeyModifiers.Control, (int)VirtualKey.A, 0) },

        ];
    }

    public override IContent[] GetContent()
    {
        var content = new MarkdownContent() { Body = _note.Details.Body };
        return content == null ? [] : [content];
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Sample code")]
public partial class EditNotePage : ContentPage
{
    private readonly Note _note;

    public EditNotePage(Note note)
    {
        _note = note;
        Name = "Quick edit";
        Title = _note.Name;
        Icon = new("\uE70F"); // edit
    }

    public override IContent[] GetContent() => [new EditNoteForm(_note)];
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Sample code")]
public partial class EditNoteForm : FormContent
{
    private readonly Note _note;

    public EditNoteForm(Note note)
    {
        _note = note;
        DataJson = $$"""
{
    "fileContent": {{JsonSerializer.Serialize(_note.NoteContent())}}
}
""";

        TemplateJson = $$"""
{
    "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
    "type": "AdaptiveCard",
    "version": "1.6",
    "body": [
        {
            "type": "Input.Text",
            "id": "Content",
            "label": "",
            "isMultiline": true,
            "value": "${fileContent}"
        }
    ],
    "actions": [
        {
            "type": "Action.Submit",
            "title": "Save",
            "data": {
                "id": "Content"
            }
        }
    ]
}
        
"""
        ;
    }

    public override ICommandResult SubmitForm(string inputs)
    {
        var formInput = JsonNode.Parse(inputs)?.AsObject();
        if (formInput == null)
        {
            return CommandResult.KeepOpen();
        }

        var fileContent = formInput["Content"];
        if (fileContent != null)
        {
            var data = fileContent.ToString();
            _note.SaveNote(data);
            ToastStatusMessage savedToast = new($"Saved {_note.Name}");
            savedToast.Message.State = MessageState.Success;
            savedToast.Show();
        }

        return CommandResult.KeepOpen();
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Sample code")]
public partial class AppendToNotePage : ContentPage
{
    private readonly Note _note;

    public AppendToNotePage(Note note)
    {
        _note = note;
        Name = "Quick add";
        Title = _note.Name;
        Icon = new("\ued0e"); // SubscriptionAdd
    }

    public override IContent[] GetContent() => [new AppendToNoteForm(_note)];
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Sample code")]
public partial class AppendToNoteForm : FormContent
{
    private readonly Note _note;

    public AppendToNoteForm(Note note)
    {
        _note = note;
        TemplateJson = $$"""
{
    "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
    "type": "AdaptiveCard",
    "version": "1.6",
    "body": [
        {
            "type": "TextBlock",
            "text": "Add to the end of the note...",
            "wrap": true
        },
        {
            "type": "Input.Text",
            "id": "Content",
            "label": "",
            "isMultiline": true
        }
    ],
    "actions": [
        {
            "type": "Action.Submit",
            "title": "Add",
            "data": {
                "id": "Content"
            }
        }
    ]
}
        
"""
        ;
    }

    public override ICommandResult SubmitForm(string inputs)
    {
        var formInput = JsonNode.Parse(inputs)?.AsObject();
        if (formInput == null)
        {
            return CommandResult.GoBack();
        }

        var fileContent = formInput["Content"];
        if (fileContent != null)
        {
            var data = fileContent.ToString();
            _note.AppendToNote(data);
        }

        return CommandResult.GoBack();
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Sample code")]
internal sealed partial class NewNoteCommand : InvokableCommand
{
    public NewNoteCommand()
    {
        Icon = ObsidianExtensionPage.ObsidianIcon;
        Name = "New note";
    }

    public override ICommandResult Invoke()
    {
        var vaultPath = SettingsManager.Instance.VaultPath;
        var vaultName = Path.GetFileName(vaultPath);
        var uri = $"obsidian://new?vault={Uri.EscapeDataString(vaultName)}";
        Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
        return CommandResult.Dismiss();
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Sample code")]
internal sealed partial class OpenDailyNoteCommand : InvokableCommand
{
    public OpenDailyNoteCommand()
    {
        Icon = ObsidianExtensionPage.ObsidianIcon;
        Name = "Open";
    }

    public override ICommandResult Invoke()
    {
        var vaultPath = SettingsManager.Instance.VaultPath;
        var vaultName = Path.GetFileName(vaultPath);
        var uri = $"obsidian://daily?vault={Uri.EscapeDataString(vaultName)}";
        Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
        return CommandResult.Dismiss();
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Sample code")]
public sealed class Note(string absolutePath)
{
    public string VaultPath { get; set; } = string.Empty;

    public string VaultName => Path.GetFileName(VaultPath);

    public string AbsolutePath { get; set; } = absolutePath;

    public DateTime LastModified { get; } = File.GetLastWriteTimeUtc(absolutePath);

    public string Name => Path.GetFileNameWithoutExtension(AbsolutePath);

    public string Folder => Path.GetDirectoryName(AbsolutePath[VaultPath.Length..].TrimStart('\\', '/'));

    public string RelativePath => Path.GetRelativePath(VaultPath, AbsolutePath);

    public string ObsidianProtocolUri => $"obsidian://open?vault={Uri.EscapeDataString(VaultName)}&file={Uri.EscapeDataString(RelativePath)}";

    public event TypedEventHandler<Note, string> ContentChanged;

    public Details Details { get; } = new NoteDetails(absolutePath) { Title = Path.GetFileNameWithoutExtension(absolutePath) };

    public string NoteContent() => Details.Body;

    public void SaveNote(string newContent)
    {
        File.WriteAllText(AbsolutePath, newContent);
        ContentChanged?.Invoke(this, null);
    }

    public void AppendToNote(string newContent)
    {
        // The Markdown text block that cmdpal is using has a weirdly hard time
        // with newlines. like, literally, \n's. If you put one in the file,
        // then it renders the content from the last \n?
        // And Obsidian will render a <br> for a single \r, but the preview won't
        File.AppendAllText(AbsolutePath, $"\r\r{newContent}");
        ContentChanged?.Invoke(this, null);
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Sample code")]
internal sealed partial class NoteDetails : Details
{
    public override string Body { get => _fileContents.Value; set => base.Body = value; }

    private readonly Lazy<string> _fileContents;
    private readonly string _absolutePath;

    private string NoteContent()
    {
        try
        {
            var content = File.ReadAllText(_absolutePath);
            return content;
        }
        catch
        {
        }

        return null;
    }

    internal NoteDetails(string absolutePath)
    {
        _absolutePath = absolutePath;
        _fileContents = new(NoteContent);
    }
}
