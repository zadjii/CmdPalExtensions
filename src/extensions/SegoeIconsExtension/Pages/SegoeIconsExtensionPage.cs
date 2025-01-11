// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CmdPal.Extensions;
using Microsoft.CmdPal.Extensions.Helpers;

namespace SegoeIconsExtension;

internal sealed partial class SegoeIconsExtensionPage : ListPage
{
    private IListItem[]? _items;

    public SegoeIconsExtensionPage()
    {
        Icon = new(string.Empty);
        Name = "Segoe Icons";
        IsLoading = true;
    }

    public override IListItem[] GetItems()
    {
        if (_items == null)
        {
            // Lazy load - only generate the list items when we're first called
            // We don't need to do anything async, because we already loaded
            // the file
            _ = Task.Run(GenerateIconItems);
            return [];

            // GenerateIconItems().Start();
        }

        IsLoading = false;
        return _items ?? [];
    }

    private async Task GenerateIconItems()
    {
        var rawIcons = await IconsDataSource.Instance.LoadIcons()!;
        _items = rawIcons.Select(ToItem).ToArray();
        IsLoading = false;
        RaiseItemsChanged(_items.Length);
    }

    private IconListItem ToItem(IconData d) => new(d);
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "I like it")]
internal sealed partial class IconListItem : ListItem
{
    private readonly IconData _data;

    public IconListItem(IconData data)
        : base(new CopyTextCommand(data.CodeGlyph))
    {
        _data = data;
        this.Title = _data.Name;
        this.Icon = new(data.Character);
        this.Subtitle = _data.CodeGlyph;
        if (data.Tags != null && data.Tags.Length > 0)
        {
            this.Tags = data.Tags.Select(t => new Tag() { Text = t }).ToArray();
        }

        this.MoreCommands =
        [
            new CommandContextItem(new CopyTextCommand(data.Character)),
            new CommandContextItem(new CopyTextCommand(data.TextGlyph)),
            new CommandContextItem(new CopyTextCommand(data.Name)),
        ];
    }
}
