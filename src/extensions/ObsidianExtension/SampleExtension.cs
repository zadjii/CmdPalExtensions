// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CommandPalette.Extensions;

namespace ObsidianExtension;

[ComVisible(true)]
[Guid("e9bf5214-f120-4622-9d6b-dd79c98e1d96")]
[ComDefaultInterface(typeof(IExtension))]
public sealed partial class SampleExtension : IExtension, IDisposable
{
    private readonly ManualResetEvent _extensionDisposedEvent;

    private readonly ObsidianExtensionActionsProvider _provider = new();

    public SampleExtension(ManualResetEvent extensionDisposedEvent)
    {
        this._extensionDisposedEvent = extensionDisposedEvent;
    }

    public object GetProvider(ProviderType providerType)
    {
        switch (providerType)
        {
            case ProviderType.Commands:
                return _provider;
            default:
                return null;
        }
    }

    public void Dispose()
    {
        this._extensionDisposedEvent.Set();
    }
}
