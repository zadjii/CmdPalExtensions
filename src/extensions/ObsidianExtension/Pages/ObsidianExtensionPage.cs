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

namespace ObsidianExtension;

internal sealed partial class ObsidianExtensionPage : ListPage
{
    public ObsidianExtensionPage()
    {
        // Icon = new(@"https://obsidian.md/images/obsidian-logo-gradient.svg");
        Icon = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory.ToString(), "Assets\\obsidian-logo.png"));
        Name = "Obsidian Notes";
        PlaceholderText = "Search notes...";
        IsLoading = true;
        ShowDetails = true;

        SettingsManager.Instance.Settings.SettingsChanged += (s, e) => RaiseItemsChanged(0);
    }

    public override IListItem[] GetItems()
    {
        var vaultPath = SettingsManager.Instance.VaultPath; // @"C:\Users\zadji\Obsidian\Notes";
        if (string.IsNullOrEmpty(vaultPath)
            || !Directory.Exists(vaultPath))
        {
            var item = new ListItem(new SettingsPage())
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

            return new ListItem(previewNote)
            {
                Title = n.Name,
                Subtitle = $"{n.Folder}/",
                MoreCommands = [
                    new CommandContextItem(openNote),
                    new CommandContextItem(new EditNotePage(n)),
                    new CommandContextItem(new AppendToNotePage(n)),
                ],
                Details = new Details()
                {
                    Title = n.Name,
                    Body = n.NoteContent(),
                },
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
public partial class PreviewNotePage : MarkdownPage
{
    private readonly Note _note;

    public PreviewNotePage(Note note)
    {
        _note = note;
        Name = "Preview";
        Title = _note.Name;
        Icon = new("\uE70B");
    }

    public override string[] Bodies()
    {
        var content = _note.NoteContent();
        return content == null ? [] : [content];
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Sample code")]
public partial class EditNotePage : FormPage
{
    private readonly Note _note;

    public EditNotePage(Note note)
    {
        _note = note;
        Name = "Quick edit";
        Title = _note.Name;
        Icon = new("\uE70F"); // edit
    }

    public override IForm[] Forms() => [new EditNoteForm(_note)];
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Sample code")]
public partial class EditNoteForm : Form
{
    private readonly Note _note;

    public EditNoteForm(Note note)
    {
        _note = note;
        Data = $$"""
{
    "fileContent": {{JsonSerializer.Serialize(_note.NoteContent())}}
}
""";

        Template = $$"""
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

    public override ICommandResult SubmitForm(string payload)
    {
        var formInput = JsonNode.Parse(payload)?.AsObject();
        if (formInput == null)
        {
            return CommandResult.KeepOpen();
        }

        var fileContent = formInput["Content"];
        if (fileContent != null)
        {
            var data = fileContent.ToString();
            _note.SaveNote(data);
        }

        return CommandResult.KeepOpen();
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Sample code")]
public partial class AppendToNotePage : FormPage
{
    private readonly Note _note;

    public AppendToNotePage(Note note)
    {
        _note = note;
        Name = "Quick add";
        Title = _note.Name;
        Icon = new("\ued0e"); // SubscriptionAdd
    }

    public override IForm[] Forms() => [new AppendToNoteForm(_note)];
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Sample code")]
public partial class AppendToNoteForm : Form
{
    private readonly Note _note;

    public AppendToNoteForm(Note note)
    {
        _note = note;
        Template = $$"""
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

    public override ICommandResult SubmitForm(string payload)
    {
        var formInput = JsonNode.Parse(payload)?.AsObject();
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
        Icon = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory.ToString(), "Assets\\obsidian-logo.png"));
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
        Icon = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory.ToString(), "Assets\\obsidian-logo.png"));
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
public partial class SettingsPage : FormPage
{
    public SettingsPage()
    {
        Name = "Settings";
        Icon = new("\uE713"); // Settings
    }

    public override IForm[] Forms() => SettingsManager.Instance.Settings.ToForms();
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

    public string NoteContent()
    {
        try
        {
            var content = File.ReadAllText(AbsolutePath);
            return content;
        }
        catch
        {
        }

        return null;
    }

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
