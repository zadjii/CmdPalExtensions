// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MastodonExtension;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(List<MastodonStatus>))]
[JsonSerializable(typeof(MastodonContext))]
[JsonSerializable(typeof(AppSecrets))]
[JsonSerializable(typeof(UserAuthToken))]
[JsonSerializable(typeof(string))]
internal sealed partial class MastodonJsonContext : JsonSerializerContext
{
}
