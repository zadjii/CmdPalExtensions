// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HackerNewsExtension.Data;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(List<int>))]
[JsonSerializable(typeof(NewsItem))]
internal sealed partial class HackerNewsJsonContext : JsonSerializerContext
{
}
