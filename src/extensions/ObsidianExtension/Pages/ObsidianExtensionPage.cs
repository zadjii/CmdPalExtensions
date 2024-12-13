// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CmdPal.Extensions;
using Microsoft.CmdPal.Extensions.Helpers;

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
    }

    public override IListItem[] GetItems()
    {
        var vaultPath = @"C:\Users\zadji\Obsidian\Notes";
        var notes = GetNotes(vaultPath);
        IsLoading = false;

        // Vault name (use the last folder in the vault path)
        var vaultName = Path.GetFileName(vaultPath);

        var listItems = notes.Select(n =>
        {
            var obsidianUri = $"obsidian://open?vault={vaultName}&file={Uri.EscapeDataString(n.RelativePath)}";
            return new ListItem(new OpenUrlCommand(obsidianUri))
            {
                Icon = new("\uE70B"),
                Title = n.Name,
                Subtitle = $"{n.Folder}/",
            };
        });
        return listItems.ToArray();
    }

    private List<Note> GetNotes(string vaultPath)
    {
        var notes = new List<Note>();

        if (Directory.Exists(vaultPath))
        {
            // Get all markdown files in the directory and subdirectories
            var noteFiles = Directory.GetFiles(vaultPath, "*.md", SearchOption.AllDirectories);

            foreach (var noteFile in noteFiles)
            {
                // Display the note name without the full path or file extension
                var noteName = Path.GetFileNameWithoutExtension(noteFile);
                var folderPath = Path.GetDirectoryName(noteFile[vaultPath.Length..].TrimStart('\\', '/'));
                var relativePath = Path.GetRelativePath(vaultPath, noteFile);
                notes.Add(new Note()
                {
                    Name = noteName,
                    Folder = folderPath,
                    RelativePath = relativePath,
                });

                // Console.WriteLine(noteName);
            }

            // Console.WriteLine($"\nTotal notes found: {noteFiles.Length}");
        }

        return notes;
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Sample code")]
public sealed class Note
{
    public string Name { get; set; } = string.Empty;

    public string Folder { get; set; } = string.Empty;

    public string RelativePath { get; set; } = string.Empty;
}
