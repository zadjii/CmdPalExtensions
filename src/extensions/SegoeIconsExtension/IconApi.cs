// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace SegoeIconsExtension;

// very shamelessly from
// https://github.com/microsoft/WinUI-Gallery/blob/main/WinUIGallery/DataModel/IconsDataSource.cs
[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "I like it")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "whatever")]
public class IconData
{
    public required string Name { get; set; }

    public required string Code { get; set; }

    public string[] Tags { get; set; } = [];

    public string Character => char.ConvertFromUtf32(Convert.ToInt32(Code, 16));

    public string CodeGlyph => "\\u" + Code;

    public string TextGlyph => "&#x" + Code + ";";
}

// [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "I like it")]
// [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
// [JsonSerializable(typeof(List<IconData>))]
// internal partial class IconDataListContext : JsonSerializerContext
// {
//    protected override JsonSerializerOptions? GeneratedSerializerOptions => throw new NotImplementedException();

// public override JsonTypeInfo? GetTypeInfo(Type type) => throw new NotImplementedException();
// }
[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "I like it")]
internal sealed class IconsDataSource
{
    public static IconsDataSource Instance { get; } = new();

    public static List<IconData> Icons => Instance.icons;

    private List<IconData> icons = [];

    private IconsDataSource()
    {
    }

    private readonly object _lock = new();

    public async Task<List<IconData>> LoadIcons()
    {
        lock (_lock)
        {
            if (icons.Count != 0)
            {
                return icons;
            }
        }

        var jsonText = await LoadText("Assets/icons.json");
        lock (_lock)
        {
            if (icons.Count == 0 &&
                !string.IsNullOrEmpty(jsonText))
            {
                icons = JsonSerializer.Deserialize<List<IconData>>(jsonText) is List<IconData> i ? i
                    : throw new InvalidDataException($"Cannot load icon data: {jsonText}");
            }

            return icons;
        }
    }

    public static async Task<string> LoadText(string relativeFilePath)
    {
        // if the file exists, load it and append the new item
        var sourcePath = Path.GetFullPath(
            Path.Combine(
                Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!,
                relativeFilePath));

        return File.Exists(sourcePath) ? await File.ReadAllTextAsync(sourcePath) : string.Empty;
    }
}
