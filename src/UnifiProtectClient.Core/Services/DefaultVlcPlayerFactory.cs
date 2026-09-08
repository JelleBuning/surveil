using LibVLCSharp.Shared;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace UnifiProtectClient.Services;

[ExcludeFromCodeCoverage]
internal sealed class DefaultVlcPlayerFactory : IVlcPlayerFactory
{
    static DefaultVlcPlayerFactory()
    {
        // Point LibVLCSharp at the native DLLs deployed by VideoLAN.LibVLC.Windows
        // (layout: <AppDir>\libvlc\win-x64\libvlc.dll + plugins\)
        var arch = Environment.Is64BitProcess ? "win-x64" : "win-x86";
        var vlcDir = Path.Combine(AppContext.BaseDirectory, "libvlc", arch);
        Core.Initialize(vlcDir);
    }

    public IVlcPlayerHandle Create(string url, Action<string> onError) =>
        new VlcPlayerHandle(url, onError);
}
