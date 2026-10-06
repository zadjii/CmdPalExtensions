// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

namespace CmdPalExtensions;

internal static class ExtensionLaunch
{
    public static void ShowHelp(string name)
    {
        // WinExe has no console when launched from Start or the Store's Open button.
        var ownsConsole = AllocConsole();
        if (!ownsConsole && Marshal.GetLastWin32Error() != 5)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to open extension launch instructions.");
        }

        try
        {
            using var output = new System.IO.StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
            output.WriteLine(name);
            output.WriteLine();
            output.WriteLine("This is an extension for PowerToys Command Palette, not a standalone app.");
            output.WriteLine("Install or update Microsoft PowerToys: https://aka.ms/powertoys");
            output.WriteLine("Enable Command Palette in PowerToys Settings, then press Win+Alt+Space");
            output.WriteLine("(or your configured shortcut) and find this extension's commands.");
            output.WriteLine("If it does not appear, check Command Palette's extension settings or restart it.");
            output.WriteLine();
            output.WriteLine("Privacy and support: https://github.com/zadjii/CmdPalExtensions");
            if (ownsConsole)
            {
                output.WriteLine("This window closes in 10 seconds.");
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }
        finally
        {
            if (ownsConsole && !FreeConsole())
            {
                Console.Error.WriteLine(new Win32Exception(Marshal.GetLastWin32Error(), "Unable to close the extension console."));
            }
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FreeConsole();
}
